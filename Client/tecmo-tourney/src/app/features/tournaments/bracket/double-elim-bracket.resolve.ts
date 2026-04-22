import { GameStatus, GameType } from 'src/app/enums';
import { IGameResult } from 'src/app/core/models/gameResult.model';
import {
  BracketMatch,
  BracketParticipant,
  FeedRef,
  ResolvedMatch,
  ResolvedSlot
} from './double-elim-bracket.types';
import { buildDoubleEliminationMatches } from './double-elim-bracket.builder';

const MAX_PASSES = 64;

/** WB1, WB2, … in visual order: earlier rounds first, then left→right within a round. */
export function buildWbGameOrdinalMap(matches: BracketMatch[]): Map<string, number> {
  const wb = matches
    .filter((m) => m.segment === 'WB')
    .sort((a, b) => a.round - b.round || a.indexInRound - b.indexInRound);
  const map = new Map<string, number>();
  wb.forEach((m, idx) => map.set(m.id, idx + 1));
  return map;
}

function wbLoserSlotLabel(feed: FeedRef, wbOrd: Map<string, number>): string | null {
  if (feed.kind !== 'loser' || !feed.matchId?.startsWith('wb-')) return null;
  const n = wbOrd.get(feed.matchId);
  return n != null ? `WB${n}` : null;
}

function applyWbAndLbSourceLabels(matches: BracketMatch[], byId: Map<string, ResolvedMatch>) {
  const wbOrd = buildWbGameOrdinalMap(matches);
  for (const def of matches) {
    const rm = byId.get(def.id)!;
    if (def.segment === 'WB') {
      const n = wbOrd.get(def.id);
      rm.wbMatchLabel = n != null ? `WB${n}` : null;
    } else if (def.segment === 'LB') {
      rm.topSourceLabel = wbLoserSlotLabel(def.top, wbOrd);
      rm.bottomSourceLabel = wbLoserSlotLabel(def.bottom, wbOrd);
    }
  }
}

function samePair(a: number, b: number, p1: number, p2: number): boolean {
  return (a === p1 && b === p2) || (a === p2 && b === p1);
}

function slotFromParticipant(p: BracketParticipant | null, isBye: boolean): ResolvedSlot {
  return { participant: p, isBye };
}

function participantById(ents: BracketParticipant[], id: number | null): BracketParticipant | null {
  if (id == null) return null;
  return ents.find((e) => e.playerId === id) ?? null;
}

function resolveFeedToSlot(
  feed: FeedRef,
  byId: Map<string, ResolvedMatch>,
  entrants: BracketParticipant[]
): ResolvedSlot {
  if (feed.kind === 'seed' && feed.seedSlot != null) {
    const ent = entrants[feed.seedSlot];
    return ent ? slotFromParticipant(ent, false) : { participant: null, isBye: true };
  }
  if (feed.kind === 'bye') {
    return { participant: null, isBye: true };
  }
  if (feed.kind === 'empty') {
    return { participant: null };
  }
  if ((feed.kind === 'winner' || feed.kind === 'loser') && feed.matchId) {
    const m = byId.get(feed.matchId);
    if (!m) return { participant: null };
    const wid = m.winnerId;
    if (wid == null) return { participant: null };
    const topId = m.top.participant?.playerId ?? null;
    const botId = m.bottom.participant?.playerId ?? null;
    const loseId =
      topId != null && botId != null ? (wid === topId ? botId : topId) : null;
    const id = feed.kind === 'winner' ? wid : loseId;
    if (id == null) return { participant: null };
    return slotFromParticipant(participantById(entrants, id), false);
  }
  return { participant: null };
}

/** Only explicit bracket byes auto-advance. Empty/TBD slots are not byes. */
function byeWinner(top: ResolvedSlot, bottom: ResolvedSlot): number | null {
  if (top.isBye && bottom.participant) return bottom.participant.playerId;
  if (bottom.isBye && top.participant) return top.participant.playerId;
  return null;
}

/** Structural bye slot: explicit bye or seed past entrant count N (matches builder wbLeafFeed). */
function feedIsStructuralBye(feed: FeedRef, entrantCount: number): boolean {
  if (feed.kind === 'bye') return true;
  if (feed.kind === 'seed' && feed.seedSlot != null) return feed.seedSlot >= entrantCount;
  return false;
}

/**
 * True if this feed can eventually supply a human to resolveFeedToSlot (winner/loser/seed),
 * independent of game rows — detects feeders that can never fill.
 */
function feedCanEventuallyProduceHuman(
  feed: FeedRef,
  canWin: Map<string, boolean>,
  canLose: Map<string, boolean>,
  entrantCount: number
): boolean {
  if (feed.kind === 'seed' && feed.seedSlot != null) return feed.seedSlot < entrantCount;
  if (feed.kind === 'bye') return false;
  if (feed.kind === 'empty') return false;
  if (feed.kind === 'winner' && feed.matchId) return canWin.get(feed.matchId) ?? false;
  if (feed.kind === 'loser' && feed.matchId) return canLose.get(feed.matchId) ?? false;
  return false;
}

/**
 * Precompute whether each match can ever declare a winner, and each WB match can ever declare a loser,
 * in dependency order (WB rounds → LB rounds → GF).
 */
function precomputeStructuralCapabilities(
  matches: BracketMatch[],
  entrantCount: number
): { canWin: Map<string, boolean>; canLose: Map<string, boolean> } {
  const canWin = new Map<string, boolean>();
  const canLose = new Map<string, boolean>();

  const wb = matches
    .filter((m) => m.segment === 'WB')
    .sort((a, b) => a.round - b.round || a.indexInRound - b.indexInRound);
  const lb = matches
    .filter((m) => m.segment === 'LB')
    .sort((a, b) => a.round - b.round || a.indexInRound - b.indexInRound);
  const gf = matches
    .filter((m) => m.segment === 'GF')
    .sort((a, b) => a.round - b.round || a.indexInRound - b.indexInRound);

  for (const def of wb) {
    const tH = feedCanEventuallyProduceHuman(def.top, canWin, canLose, entrantCount);
    const bH = feedCanEventuallyProduceHuman(def.bottom, canWin, canLose, entrantCount);
    const tB = feedIsStructuralBye(def.top, entrantCount);
    const bB = feedIsStructuralBye(def.bottom, entrantCount);
    const win = (tH && bH) || (tH && bB) || (tB && bH);
    canWin.set(def.id, win);
    canLose.set(def.id, tH && bH);
  }

  for (const def of lb) {
    const ta = feedCanEventuallyProduceHuman(def.top, canWin, canLose, entrantCount);
    const tb = feedCanEventuallyProduceHuman(def.bottom, canWin, canLose, entrantCount);
    canWin.set(def.id, ta || tb);
  }

  for (const def of gf) {
    if (def.id === 'gf-1-0') {
      canWin.set(def.id, false);
      continue;
    }
    const ta = feedCanEventuallyProduceHuman(def.top, canWin, canLose, entrantCount);
    const tb = feedCanEventuallyProduceHuman(def.bottom, canWin, canLose, entrantCount);
    canWin.set(def.id, ta || tb);
  }

  return { canWin, canLose };
}

function feedCanNeverProduceParticipant(
  feed: FeedRef,
  canWin: Map<string, boolean>,
  canLose: Map<string, boolean>,
  entrantCount: number
): boolean {
  return !feedCanEventuallyProduceHuman(feed, canWin, canLose, entrantCount);
}

/**
 * One side has a player, the other slot is still empty, and the empty side's feeder can never
 * supply anyone — advance the lone player (all LB rounds + first grand final).
 */
function soleBracketAdvanceFromUnreachableFeed(
  top: ResolvedSlot,
  bottom: ResolvedSlot,
  def: BracketMatch,
  canWin: Map<string, boolean>,
  canLose: Map<string, boolean>,
  entrantCount: number
): number | null {
  if (def.segment !== 'LB' && !(def.segment === 'GF' && def.id === 'gf-0-0')) return null;
  if (top.isBye || bottom.isBye) return null;
  const t = top.participant?.playerId ?? null;
  const b = bottom.participant?.playerId ?? null;
  if (t != null && b != null) return null;
  if (t != null && b == null && feedCanNeverProduceParticipant(def.bottom, canWin, canLose, entrantCount)) {
    return t;
  }
  if (t == null && b != null && feedCanNeverProduceParticipant(def.top, canWin, canLose, entrantCount)) {
    return b;
  }
  return null;
}

function structuralAutoWinner(
  top: ResolvedSlot,
  bottom: ResolvedSlot,
  def: BracketMatch,
  canWin: Map<string, boolean>,
  canLose: Map<string, boolean>,
  entrantCount: number
): number | null {
  const bw = byeWinner(top, bottom);
  if (bw != null) return bw;
  return soleBracketAdvanceFromUnreachableFeed(top, bottom, def, canWin, canLose, entrantCount);
}

function winnerFromCompletedGame(g: IGameResult): number | null {
  if (g.status !== GameStatus.Completed) return null;
  const s1 = g.player1.score;
  const s2 = g.player2.score;
  if (s1 > s2) return g.player1.playerId;
  if (s2 > s1) return g.player2.playerId;
  return null;
}

/** Prefer Winners earliest rounds, then Losers, then Grand Finals when multiple slots match. */
function bracketPriority(def: BracketMatch): [number, number, number, number] {
  if (def.segment === 'WB') {
    return [0, def.round, def.indexInRound, 0];
  }
  if (def.segment === 'LB') {
    return [1, def.round, def.indexInRound, 0];
  }
  return [2, def.round, def.indexInRound, 0];
}

function cmpPri(a: [number, number, number, number], b: [number, number, number, number]): number {
  for (let i = 0; i < 4; i++) {
    const d = a[i] - b[i];
    if (d !== 0) return d;
  }
  return 0;
}

export function buildEntrantsFromStandings(
  standings: { playerId: number; playerName: string; preliminaryPosition: number }[]
): BracketParticipant[] {
  const sorted = [...standings].sort((a, b) => a.preliminaryPosition - b.preliminaryPosition);
  return sorted.map((s) => ({
    playerId: s.playerId,
    name: s.playerName,
    seed: s.preliminaryPosition
  }));
}

/**
 * Prefer API standings (authoritative seeds). If standings are not loaded yet or empty,
 * fall back to tournament roster: stable order by playerId, synthetic seeds 1..N.
 */
export function buildBracketEntrants(
  standings: { playerId: number; playerName: string; preliminaryPosition: number }[],
  players: { playerId: number; fullName: string }[]
): BracketParticipant[] {
  if (standings?.length) {
    return buildEntrantsFromStandings(standings);
  }
  const sorted = [...(players ?? [])].sort((a, b) => a.playerId - b.playerId);
  return sorted.map((p, i) => ({
    playerId: p.playerId,
    name: p.fullName,
    seed: i + 1
  }));
}

export function resolveDoubleElimination(
  entrantCount: number,
  entrants: BracketParticipant[],
  tournamentGames: IGameResult[]
): { B: number; resolved: ResolvedMatch[]; matches: BracketMatch[] } {
  const { B, matches } = buildDoubleEliminationMatches(entrantCount);
  const bracketGames = tournamentGames
    .filter((g) => g.gameType === GameType.Tournament)
    .sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());

  const byId = new Map<string, ResolvedMatch>();
  for (const def of matches) {
    byId.set(def.id, {
      def,
      top: { participant: null },
      bottom: { participant: null },
      gameResultId: null,
      status: null,
      topScore: null,
      bottomScore: null,
      winnerId: null,
      isPending: false,
      topSourceLabel: null,
      bottomSourceLabel: null,
      wbMatchLabel: null
    });
  }

  applyWbAndLbSourceLabels(matches, byId);

  const gamesById = new Map(bracketGames.map((g) => [g.gameResultId, g]));
  const caps = precomputeStructuralCapabilities(matches, entrantCount);

  const fullPropagate = () => {
    for (let pass = 0; pass < MAX_PASSES; pass++) {
      let changed = false;
      for (const def of matches) {
        if (def.id === 'gf-1-0') continue;
        const rm = byId.get(def.id)!;
        const top = resolveFeedToSlot(def.top, byId, entrants);
        const bottom = resolveFeedToSlot(def.bottom, byId, entrants);
        if (JSON.stringify(rm.top) !== JSON.stringify(top)) {
          rm.top = top;
          changed = true;
        }
        if (JSON.stringify(rm.bottom) !== JSON.stringify(bottom)) {
          rm.bottom = bottom;
          changed = true;
        }

        const auto = structuralAutoWinner(rm.top, rm.bottom, def, caps.canWin, caps.canLose, entrantCount);
        if (auto != null && rm.winnerId !== auto) {
          rm.winnerId = auto;
          changed = true;
        }
        // No bound game: two human players cannot have a declared winner until a game exists
        // (clears stale sole-advance winnerId when the second feeder arrives).
        const h1 = rm.top.participant?.playerId ?? null;
        const h2 = rm.bottom.participant?.playerId ?? null;
        if (
          rm.gameResultId == null &&
          h1 != null &&
          h2 != null &&
          h1 !== h2 &&
          !rm.top.isBye &&
          !rm.bottom.isBye &&
          rm.winnerId !== null
        ) {
          rm.winnerId = null;
          changed = true;
        }
      }

      for (const def of matches) {
        const rm = byId.get(def.id)!;
        if (rm.gameResultId == null) continue;
        const g = gamesById.get(rm.gameResultId);
        if (!g) continue;
        rm.status = g.status;
        const pTop = rm.top.participant?.playerId;
        if (pTop != null) {
          rm.topScore = pTop === g.player1.playerId ? g.player1.score : g.player2.score;
          rm.bottomScore = pTop === g.player1.playerId ? g.player2.score : g.player1.score;
        } else {
          rm.topScore = null;
          rm.bottomScore = null;
        }
        if (g.status === GameStatus.Completed) {
          const w = winnerFromCompletedGame(g);
          if (w != null && rm.winnerId !== w) {
            rm.winnerId = w;
            changed = true;
          }
        } else if (g.status === GameStatus.Waiting || g.status === GameStatus.InProgress) {
          const structural = structuralAutoWinner(rm.top, rm.bottom, def, caps.canWin, caps.canLose, entrantCount);
          if (structural != null) {
            if (rm.winnerId !== structural) {
              rm.winnerId = structural;
              changed = true;
            }
          } else if (rm.winnerId !== null) {
            rm.winnerId = null;
            changed = true;
          }
        }
      }

      if (!changed) break;
    }
  };

  fullPropagate();

  const used = new Set<number>();

  for (const g of bracketGames) {
    if (used.has(g.gameResultId)) continue;
    fullPropagate();
    const candidates: { def: BracketMatch; rm: ResolvedMatch; pri: [number, number, number, number] }[] = [];
    for (const def of matches) {
      if (def.id === 'gf-1-0') continue;
      const rm = byId.get(def.id)!;
      if (rm.gameResultId != null) continue;
      const p1 = rm.top.participant?.playerId;
      const p2 = rm.bottom.participant?.playerId;
      if (p1 == null || p2 == null || p1 === p2) continue;
      if (!samePair(p1, p2, g.player1.playerId, g.player2.playerId)) continue;
      candidates.push({ def, rm, pri: bracketPriority(def) });
    }
    candidates.sort((a, b) => cmpPri(a.pri, b.pri));
    const pick = candidates[0];
    if (pick) {
      pick.rm.gameResultId = g.gameResultId;
      used.add(g.gameResultId);
    }
    fullPropagate();
  }

  const gf0 = byId.get('gf-0-0')!;
  const gf1 = byId.get('gf-1-0')!;

  const wbFinalTop = gf0.def.top;
  const lbFinalBot = gf0.def.bottom;
  let wbChampPlayerId: number | null = null;
  let lbChampPlayerId: number | null = null;
  if (wbFinalTop.kind === 'winner' && wbFinalTop.matchId) {
    wbChampPlayerId = byId.get(wbFinalTop.matchId)?.winnerId ?? null;
  }
  if (lbFinalBot.kind === 'winner' && lbFinalBot.matchId) {
    lbChampPlayerId = byId.get(lbFinalBot.matchId)?.winnerId ?? null;
  }

  let showReset = false;
  if (
    gf0.status === GameStatus.Completed &&
    gf0.winnerId != null &&
    lbChampPlayerId != null &&
    gf0.winnerId === lbChampPlayerId
  ) {
    showReset = true;
    const pTop = gf0.top.participant;
    const pBot = gf0.bottom.participant;
    if (pTop && pBot) {
      gf1.top = { participant: pTop };
      gf1.bottom = { participant: pBot };
      fullPropagate();
      for (const g of bracketGames) {
        if (used.has(g.gameResultId)) continue;
        if (!samePair(g.player1.playerId, g.player2.playerId, pTop.playerId, pBot.playerId)) continue;
        gf1.gameResultId = g.gameResultId;
        used.add(g.gameResultId);
        break;
      }
      fullPropagate();
      if (gf1.gameResultId == null) {
        gf1.isPending = true;
      }
    }
  }

  if (!showReset) {
    gf1.top = { participant: null };
    gf1.bottom = { participant: null };
    gf1.gameResultId = null;
    gf1.status = null;
    gf1.topScore = null;
    gf1.bottomScore = null;
    gf1.winnerId = null;
    gf1.isPending = false;
  }

  for (const def of matches) {
    const rm = byId.get(def.id)!;
    const p1 = rm.top.participant?.playerId;
    const p2 = rm.bottom.participant?.playerId;
    if (p1 != null && p2 != null && p1 !== p2) {
      rm.isPending = rm.gameResultId == null && rm.winnerId == null;
    } else {
      rm.isPending = false;
    }
  }

  const ordered = matches
    .filter((m) => m.id !== 'gf-1-0' || showReset)
    .map((m) => byId.get(m.id)!);

  return { B, resolved: ordered, matches };
}
