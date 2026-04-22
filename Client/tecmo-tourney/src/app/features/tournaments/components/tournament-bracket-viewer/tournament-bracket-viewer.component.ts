import {
  AfterViewInit,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  OnChanges,
  OnDestroy,
  Output,
  SimpleChanges,
  ViewChild
} from '@angular/core';
import { IGameResult } from 'src/app/core/models/gameResult.model';
import { IPlayer } from 'src/app/core/models/player.model';
import { IBracketOddsLine } from 'src/app/core/models/pointSpread.model';
import { ITournamentStanding } from 'src/app/core/models/tournamentStandingModel';
import { GameStatus, TournamentStatus } from 'src/app/enums';
import {
  BracketMatch,
  BracketParticipant,
  FeedRef,
  ResolvedMatch
} from '../../bracket/double-elim-bracket.types';
import {
  buildBracketEntrants,
  buildWbGameOrdinalMap,
  resolveDoubleElimination
} from '../../bracket/double-elim-bracket.resolve';

@Component({
  selector: 'app-tournament-bracket-viewer',
  standalone: false,
  templateUrl: './tournament-bracket-viewer.component.html',
  styleUrls: ['./tournament-bracket-viewer.component.less']
})
export class TournamentBracketViewerComponent implements OnChanges, AfterViewInit, OnDestroy {
  /** Expose enums for template. */
  readonly TournamentStatus = TournamentStatus;
  readonly GameStatus = GameStatus;

  @ViewChild('bracketConnectorsHost') bracketConnectorsHost?: ElementRef<HTMLElement>;
  @ViewChild('connectorSvg') connectorSvg?: ElementRef<SVGSVGElement>;

  @Input() games: IGameResult[] = [];
  /** When set, preliminaryPosition drives seeds. */
  @Input() standings: ITournamentStanding[] = [];
  /** Used when standings are empty; seeds default to 1..N by playerId order. */
  @Input() players: IPlayer[] = [];
  /** Point-spread lines for scheduled games (gameResultId → line). Slots without a game or odds show "( )". */
  @Input() oddsByGameResultId: Record<number, IBracketOddsLine> = {};
  /** When Waiting or Preliminaries, show a banner that the bracket is not open for play yet. */
  @Input() tournamentStatus: TournamentStatus | null = null;
  /** Fired when a bracket game card is activated (click or keyboard). */
  @Output() bracketMatchActivate = new EventEmitter<{
    match: ResolvedMatch;
    code: string;
  }>();

  /** Min height of `.bracket-match` (two player rows); keep in sync with LESS. */
  private readonly wbMatchCardPx = 78;
  /**
   * Vertical step per winners‑bracket slot: grey code row + gap + card.
   * Must match `.bracket-match-code` + `.bracket-game-stack` gap + card height in LESS.
   */
  private readonly wbMatchStackPx = 98;
  private readonly wbRound0GapPx = 14;
  /** LB slot height for margin math (code + gap + card); keep in sync with LESS. */
  private readonly lbMatchStackPx = 98;
  private readonly lbRound0GapPx = 14;

  /** Losers bracket display ids LB1, LB2, … in column‑major visual order. */
  private lbMatchLabelByDefId: Record<string, string> = {};
  /** For each WB match id (e.g. wb-0-1), the LB label (e.g. LB1) where that match's loser drops. */
  private wbLoserDropLbLabelByWbDefId: Record<string, string> = {};
  /** For each LB match id (e.g. lb-2-0), the label where that match's winner advances (LBn or Final). */
  private lbWinnerNextLabelByLbDefId: Record<string, string> = {};
  /** WB1…WBn per def id (same order as bracket); fallback when wbMatchLabel is missing on resolved match. */
  private wbCodeByDefId: Record<string, string> = {};

  wbColumns: ResolvedMatch[][] = [];
  /** margin-top per match; aligns later rounds halfway between feeder matches. */
  wbMatchMarginsPx: number[][] = [];
  lbColumns: ResolvedMatch[][] = [];
  /** margin-top per LB match; aligns each match between its two feeders (WB/LB) in global Y. */
  lbMatchMarginsPx: number[][] = [];
  gfMatches: ResolvedMatch[] = [];
  /** Set when GF (or GFR if played) has a decisive winner — shown beside Finals. */
  champion: { name: string } | null = null;
  bracketSize = 0;
  entrantCount = 0;
  tooFewPlayers = false;
  tooManyPlayers = false;

  /** Static bracket graph from the resolver (feeds for LB/GF connectors). */
  private bracketMatches: BracketMatch[] = [];
  /** For LB0 visibility: lookup WB matches by id (see resolveFeedToSlot loser branch in double-elim-bracket.resolve). */
  private resolvedByDefId = new Map<string, ResolvedMatch>();
  private bracketResizeObserver: ResizeObserver | null = null;
  private windowResizeRaf = 0;

  /** Coalesced window resize → redraw connectors (ResizeObserver may not fire on layout-only reflow). */
  private readonly onWindowResize = (): void => {
    if (this.windowResizeRaf) {
      cancelAnimationFrame(this.windowResizeRaf);
    }
    this.windowResizeRaf = requestAnimationFrame(() => {
      this.windowResizeRaf = 0;
      this.scheduleDrawConnectors();
    });
  };

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['games'] || changes['standings'] || changes['players'] || changes['oddsByGameResultId']) {
      this.rebuild();
      this.scheduleDrawConnectors();
    }
  }

  ngAfterViewInit(): void {
    this.scheduleDrawConnectors();
    this.observeBracketResize();
    if (typeof window !== 'undefined') {
      window.addEventListener('resize', this.onWindowResize);
    }
  }

  ngOnDestroy(): void {
    this.bracketResizeObserver?.disconnect();
    this.bracketResizeObserver = null;
    if (this.windowResizeRaf) {
      cancelAnimationFrame(this.windowResizeRaf);
      this.windowResizeRaf = 0;
    }
    if (typeof window !== 'undefined') {
      window.removeEventListener('resize', this.onWindowResize);
    }
  }

  /** Numeric score, or "-" when the bound game is still waiting (not played). */
  formatScore(m: ResolvedMatch, value: number | null | undefined): string {
    if (m.status === GameStatus.Waiting) {
      return '-';
    }
    if (value === null || value === undefined) return '';
    return String(value);
  }

  /** Shown left of bracket cards; numeric id when bound to a persisted game. */
  matchGameResultIdDisplay(m: ResolvedMatch): string {
    if (m.gameResultId != null && m.gameResultId > 0) {
      return String(m.gameResultId);
    }
    return '—';
  }

  /**
   * LB destination for this WB match's loser (e.g. LB3), from bracket structure, or null if none.
   * Does not depend on whether both participants are resolved yet — later WB rounds were hiding the arrow
   * until both feeder games filled because the old check used resolved slots only.
   */
  wbLoserDropLabel(m: ResolvedMatch): string | null {
    if (m.def.segment !== 'WB') return null;
    return this.wbLoserDropLbLabelByWbDefId[m.def.id] ?? null;
  }

  /** Tooltip: where the loser of this WB game goes. */
  wbLoserDropTitle(m: ResolvedMatch): string | null {
    const lb = this.wbLoserDropLabel(m);
    return lb ? `Loser drops to ${lb}` : null;
  }

  /** Losers bracket: where the winner advances (e.g. LB7) or Final for the last LB game. */
  lbWinnerNextLabel(m: ResolvedMatch): string | null {
    if (m.def.segment !== 'LB') {
      return null;
    }
    return this.lbWinnerNextLabelByLbDefId[m.def.id] ?? null;
  }

  lbWinnerNextTitle(m: ResolvedMatch): string | null {
    const dest = this.lbWinnerNextLabel(m);
    return dest ? `Winner advances to ${dest}` : null;
  }

  /** Visible above each card: WB1, LB3, GF, … */
  bracketCodeLabel(m: ResolvedMatch): string {
    if (m.def.segment === 'WB') {
      return m.wbMatchLabel || this.wbCodeByDefId[m.def.id] || '';
    }
    if (m.def.segment === 'LB') {
      return this.lbMatchLabelByDefId[m.def.id] ?? '';
    }
    if (m.def.segment === 'GF') {
      return m.def.round === 1 ? 'GFR' : 'GF';
    }
    return '';
  }

  /**
   * Tooltip on the camouflaged game‑id column (feeder WB#s for losers).
   * WB “Loser → LBn” uses {@link wbLoserDropLabel} (bracket graph mapping).
   */
  matchIdColumnTitle(m: ResolvedMatch): string | null {
    const code = this.bracketCodeLabel(m);
    const idTxt = this.matchGameResultIdDisplay(m);
    const bits: string[] = [];
    if (code) {
      bits.push(code);
    }
    if (idTxt !== '—') {
      bits.push(`#${idTxt}`);
    }
    if (m.def.segment === 'LB') {
      const wn = this.lbWinnerNextLabel(m);
      if (wn) {
        bits.push(`Winner → ${wn}`);
      }
      const parts = [m.topSourceLabel, m.bottomSourceLabel].filter(Boolean);
      if (parts.length) {
        bits.push(`Feeds: ${parts.join(' · ')}`);
      }
    }
    if (m.def.segment === 'WB') {
      const drop = this.wbLoserDropLabel(m);
      if (drop) {
        bits.push(`Loser → ${drop}`);
      }
    }
    return bits.length ? bits.join(' · ') : null;
  }

  /**
   * Spread label inside parentheses after the player name: "-3", "+7", or "-" when no line.
   */
  formatSpreadParenthetical(m: ResolvedMatch, side: 'top' | 'bottom'): string {
    const slot = side === 'top' ? m.top : m.bottom;
    if (!slot.participant || slot.isBye) {
      return '';
    }
    if (m.gameResultId == null || m.gameResultId < 1) {
      return '-';
    }
    const o = this.oddsByGameResultId[m.gameResultId];
    if (!o) {
      return '-';
    }
    const spread = Number(o.spread);
    if (Number.isNaN(spread)) {
      return '-';
    }
    const mag = Math.abs(spread);
    const s = mag % 1 === 0 ? String(mag) : mag.toFixed(1);
    const fav = o.favoredPlayerId;
    if (fav == null) {
      return '-';
    }
    if (fav === slot.participant.playerId) {
      return `-${s}`;
    }
    return `+${s}`;
  }

  onMatchActivateMouse(m: ResolvedMatch, ev: MouseEvent): void {
    ev.preventDefault();
    ev.stopPropagation();
    this.emitBracketMatch(m);
  }

  onMatchActivateKeydown(m: ResolvedMatch, ev: KeyboardEvent): void {
    if (ev.key === 'Enter' || ev.key === ' ') {
      ev.preventDefault();
      ev.stopPropagation();
      this.emitBracketMatch(m);
    }
  }

  private emitBracketMatch(m: ResolvedMatch): void {
    this.bracketMatchActivate.emit({ match: m, code: this.bracketCodeLabel(m) });
  }

  rowClasses(side: 'top' | 'bottom', m: ResolvedMatch): Record<string, boolean> {
    const slot = side === 'top' ? m.top : m.bottom;
    const pending =
      m.isPending ||
      (!slot.participant && !slot.isBye) ||
      (slot.participant == null && !slot.isBye);
    const dim = pending && !slot.isBye;
    return {
      'bracket-row': true,
      'bracket-row--bye': !!slot.isBye,
      'bracket-row--pending': dim
    };
  }

  private rebuild(): void {
    const entrants = buildBracketEntrants(this.standings || [], this.players || []);
    this.entrantCount = entrants.length;
    this.tooFewPlayers = entrants.length > 0 && entrants.length < 4;
    this.tooManyPlayers = entrants.length > 32;

    if (this.tooFewPlayers || this.tooManyPlayers || entrants.length === 0) {
      this.wbColumns = [];
      this.wbMatchMarginsPx = [];
      this.lbColumns = [];
      this.lbMatchMarginsPx = [];
      this.gfMatches = [];
      this.champion = null;
      this.lbMatchLabelByDefId = {};
      this.wbLoserDropLbLabelByWbDefId = {};
      this.lbWinnerNextLabelByLbDefId = {};
      this.wbCodeByDefId = {};
      this.bracketMatches = [];
      this.resolvedByDefId.clear();
      this.bracketSize = 0;
      return;
    }

    const { B, resolved, matches } = resolveDoubleElimination(entrants.length, entrants, this.games || []);
    this.bracketMatches = matches;
    this.bracketSize = B;

    const wbRounds = new Map<number, ResolvedMatch[]>();
    const lbRounds = new Map<number, ResolvedMatch[]>();
    const gf: ResolvedMatch[] = [];

    this.resolvedByDefId.clear();
    for (const rm of resolved) {
      this.resolvedByDefId.set(rm.def.id, rm);
      if (rm.def.segment === 'WB') {
        const list = wbRounds.get(rm.def.round) ?? [];
        list.push(rm);
        wbRounds.set(rm.def.round, list);
      } else if (rm.def.segment === 'LB') {
        const list = lbRounds.get(rm.def.round) ?? [];
        list.push(rm);
        lbRounds.set(rm.def.round, list);
      } else if (rm.def.segment === 'GF') {
        gf.push(rm);
      }
    }

    this.wbColumns = [...wbRounds.keys()]
      .sort((a, b) => a - b)
      .map((r) => (wbRounds.get(r) ?? []).sort((a, b) => a.def.indexInRound - b.def.indexInRound));

    const wbTopY = this.computeWbTopY(this.wbColumns);
    this.wbMatchMarginsPx = this.computeMarginsFromTopY(wbTopY, this.wbMatchStackPx);

    this.lbColumns = [...lbRounds.keys()]
      .sort((a, b) => a - b)
      .map((r) =>
        (lbRounds.get(r) ?? [])
          .sort((a, b) => a.def.indexInRound - b.def.indexInRound)
          .filter((m) => !this.isLbR0PermanentlyUnreachable(m))
      );

    this.lbMatchMarginsPx = this.computeLbMatchMargins(this.lbColumns, wbTopY);

    this.lbMatchLabelByDefId = {};
    let lbOrd = 1;
    for (const col of this.lbColumns) {
      for (const rm of col) {
        this.lbMatchLabelByDefId[rm.def.id] = `LB${lbOrd++}`;
      }
    }

    this.buildWbLoserDropLabels(matches);
    this.buildLbWinnerDestinationLabels(matches);

    this.gfMatches = gf.sort((a, b) => a.def.round - b.def.round);
    this.computeChampion(resolved, entrants);
  }

  /**
   * True when this WB matchup can never supply a `loser` feed (resolveFeedToSlot requires two human
   * playerIds to compute loseId). Covers bye-vs-bye and player-vs-bye; not two humans.
   */
  private wbCanNeverProduceLoser(wb: ResolvedMatch): boolean {
    const topId = wb.top.participant?.playerId ?? null;
    const botId = wb.bottom.participant?.playerId ?? null;
    return !(topId != null && botId != null);
  }

  /**
   * LB round 0: two WB losers feed in. If both feeder WBs can never produce a loser (see
   * wbCanNeverProduceLoser), no one can ever enter this slot — omit from the UI.
   */
  private isLbR0PermanentlyUnreachable(m: ResolvedMatch): boolean {
    if (m.def.segment !== 'LB' || m.def.round !== 0) {
      return false;
    }
    const t = m.def.top;
    const b = m.def.bottom;
    if (t.kind !== 'loser' || !t.matchId || b.kind !== 'loser' || !b.matchId) {
      return false;
    }
    const wbA = this.resolvedByDefId.get(t.matchId);
    const wbB = this.resolvedByDefId.get(b.matchId);
    if (!wbA || !wbB) {
      return false;
    }
    return this.wbCanNeverProduceLoser(wbA) && this.wbCanNeverProduceLoser(wbB);
  }

  /**
   * Maps each wb-* id to the display label (LBn) of the losers-bracket game that receives that WB loser.
   */
  private buildWbLoserDropLabels(matches: BracketMatch[]): void {
    this.wbLoserDropLbLabelByWbDefId = {};
    for (const def of matches) {
      if (def.segment !== 'LB') continue;
      const lbLabel = this.lbMatchLabelByDefId[def.id];
      if (!lbLabel) continue;
      const mark = (feed: FeedRef) => {
        if (feed.kind === 'loser' && feed.matchId) {
          this.wbLoserDropLbLabelByWbDefId[feed.matchId] = lbLabel;
        }
      };
      mark(def.top);
      mark(def.bottom);
    }
  }

  /**
   * For each LB game, the display label where its winner goes — next LB code (LBn) or Final (into first grand final).
   */
  private buildLbWinnerDestinationLabels(matches: BracketMatch[]): void {
    this.lbWinnerNextLabelByLbDefId = {};
    for (const def of matches) {
      for (const feed of [def.top, def.bottom]) {
        if (feed.kind !== 'winner' || !feed.matchId?.startsWith('lb-')) {
          continue;
        }
        const srcLbId = feed.matchId;
        let dest: string;
        if (def.segment === 'GF' && def.id === 'gf-0-0') {
          dest = 'Final';
        } else if (def.segment === 'LB') {
          dest = this.lbMatchLabelByDefId[def.id] ?? '';
        } else {
          continue;
        }
        if (dest) {
          this.lbWinnerNextLabelByLbDefId[srcLbId] = dest;
        }
      }
    }
  }

  /** Winner after GF, or after GFR when a reset was required. */
  private computeChampion(resolved: ResolvedMatch[], entrants: BracketParticipant[]): void {
    const gf0 = resolved.find((r) => r.def.segment === 'GF' && r.def.round === 0);
    const gf1 = resolved.find((r) => r.def.segment === 'GF' && r.def.round === 1);
    if (gf1) {
      if (gf1.status === GameStatus.Completed && gf1.winnerId != null) {
        const ent = entrants.find((e) => e.playerId === gf1.winnerId);
        this.champion = ent ? { name: ent.name } : null;
      } else {
        this.champion = null;
      }
      return;
    }
    if (gf0?.status === GameStatus.Completed && gf0.winnerId != null) {
      const ent = entrants.find((e) => e.playerId === gf0.winnerId);
      this.champion = ent ? { name: ent.name } : null;
      return;
    }
    this.champion = null;
  }

  /**
   * Winners bracket: each match top Y = midpoint between feeder tops (classic tree).
   */
  private computeWbTopY(columns: ResolvedMatch[][]): number[][] {
    if (!columns.length) return [];
    const H = this.wbMatchStackPx;
    const G = this.wbRound0GapPx;
    const unit = H + G;
    const rounds = columns.length;
    const topY: number[][] = [];

    for (let r = 0; r < rounds; r++) {
      const n = columns[r].length;
      topY[r] = [];
      for (let i = 0; i < n; i++) {
        if (r === 0) {
          topY[r][i] = i * unit;
        } else {
          topY[r][i] = (topY[r - 1][2 * i] + topY[r - 1][2 * i + 1]) / 2;
        }
      }
    }
    return topY;
  }

  private computeMarginsFromTopY(topY: number[][], H: number): number[][] {
    const margins: number[][] = [];
    for (let r = 0; r < topY.length; r++) {
      margins[r] = [];
      for (let i = 0; i < topY[r].length; i++) {
        margins[r][i] = i === 0 ? topY[r][i] : topY[r][i] - topY[r][i - 1] - H;
      }
    }
    return margins;
  }

  /**
   * Losers bracket: j=0 spaces lb-0-i by WB pair midpoints (wb-0-(2i), wb-0-(2i+1)), shifted so the
   * first row starts at 0 (DOM is below WB; relative spacing matches the winners column). j≥1 centers
   * between feeders. Uses WB tops at 0; lbBase for j≥1 math only.
   */
  private computeLbMatchMargins(lbColumns: ResolvedMatch[][], wbTopY: number[][]): number[][] {
    if (!lbColumns.length) return [];
    const H = this.lbMatchStackPx;
    const G = this.lbRound0GapPx;
    const unit = H + G;
    const hWb = this.wbMatchStackPx;
    /** Keep in sync with `.bracket-section { margin-bottom }` (space before Losers bracket). */
    const sectionGapAfterWb = 28;

    let wbMaxBottom = 0;
    for (let r = 0; r < wbTopY.length; r++) {
      for (let i = 0; i < wbTopY[r].length; i++) {
        wbMaxBottom = Math.max(wbMaxBottom, wbTopY[r][i] + hWb);
      }
    }
    const lbBase = wbMaxBottom + sectionGapAfterWb;

    /** LB round-0 local tops in WB Y space (midpoint of each WB pair minus half card height). */
    const lb0RawTop: Record<number, number> = {};
    const wb0 = wbTopY[0];
    if (wb0 && lbColumns[0]?.length) {
      for (const rm of lbColumns[0]) {
        const ii = rm.def.indexInRound;
        const ti = 2 * ii;
        const bi = 2 * ii + 1;
        if (wb0[ti] !== undefined && wb0[bi] !== undefined) {
          const midCenter = (wb0[ti] + hWb / 2 + wb0[bi] + hWb / 2) / 2;
          lb0RawTop[ii] = midCenter - H / 2;
        }
      }
    }
    const lb0RawVals = Object.values(lb0RawTop);
    const lb0MinRaw = lb0RawVals.length ? Math.min(...lb0RawVals) : 0;

    const lbTopY: number[][] = [];

    for (let j = 0; j < lbColumns.length; j++) {
      const sorted = [...lbColumns[j]].sort((a, b) => a.def.indexInRound - b.def.indexInRound);
      lbTopY[j] = [];

      for (let k = 0; k < sorted.length; k++) {
        const rm = sorted[k];
        const i = rm.def.indexInRound;
        const def = rm.def;

        if (j === 0) {
          if (lb0RawTop[i] !== undefined) {
            lbTopY[j][i] = lb0RawTop[i]! - lb0MinRaw;
          } else {
            lbTopY[j][i] = k * unit;
          }
          continue;
        }

        const idTop = def.top.matchId;
        const idBot = def.bottom.matchId;
        const bothLbFeeds =
          idTop?.startsWith('lb-') &&
          idBot?.startsWith('lb-') &&
          (def.top.kind === 'winner' || def.top.kind === 'loser') &&
          (def.bottom.kind === 'winner' || def.bottom.kind === 'loser');

        if (!bothLbFeeds) {
          lbTopY[j][i] = k * unit;
          continue;
        }

        const cTop = this.feedMatchCenterY(def.top, wbTopY, lbTopY, lbBase, hWb, H);
        const cBot = this.feedMatchCenterY(def.bottom, wbTopY, lbTopY, lbBase, hWb, H);
        if (cTop === null || cBot === null) {
          lbTopY[j][i] = k * unit;
          continue;
        }
        const center = (cTop + cBot) / 2;
        lbTopY[j][i] = center - H / 2 - lbBase;
      }
    }

    const margins: number[][] = [];
    for (let j = 0; j < lbColumns.length; j++) {
      const sorted = [...lbColumns[j]].sort((a, b) => a.def.indexInRound - b.def.indexInRound);
      margins[j] = [];
      for (let k = 0; k < sorted.length; k++) {
        const i = sorted[k].def.indexInRound;
        const prevI = k === 0 ? null : sorted[k - 1].def.indexInRound;
        margins[j][k] =
          k === 0 ? lbTopY[j][i] : lbTopY[j][i] - lbTopY[j][prevI!] - H;
      }
    }
    return margins;
  }

  /** Global Y center of a feeder match (WB area origin 0; LB uses lbBase + local top). */
  private feedMatchCenterY(
    feed: FeedRef,
    wbTopY: number[][],
    lbTopY: number[][],
    lbBase: number,
    hWb: number,
    hLb: number
  ): number | null {
    if ((feed.kind !== 'winner' && feed.kind !== 'loser') || !feed.matchId) {
      return null;
    }
    const id = feed.matchId;
    const wbM = /^wb-(\d+)-(\d+)$/.exec(id);
    if (wbM) {
      const r = parseInt(wbM[1], 10);
      const idx = parseInt(wbM[2], 10);
      if (!wbTopY[r] || wbTopY[r][idx] === undefined) return null;
      return wbTopY[r][idx] + hWb / 2;
    }
    const lbM = /^lb-(\d+)-(\d+)$/.exec(id);
    if (lbM) {
      const jr = parseInt(lbM[1], 10);
      const idx = parseInt(lbM[2], 10);
      if (!lbTopY[jr] || lbTopY[jr][idx] === undefined) return null;
      return lbBase + lbTopY[jr][idx] + hLb / 2;
    }
    return null;
  }

  private observeBracketResize(): void {
    const host = this.bracketConnectorsHost?.nativeElement;
    if (!host || typeof ResizeObserver === 'undefined') return;
    this.bracketResizeObserver = new ResizeObserver(() => this.scheduleDrawConnectors());
    this.bracketResizeObserver.observe(host);
  }

  private scheduleDrawConnectors(): void {
    requestAnimationFrame(() => {
      setTimeout(() => this.drawConnectors(), 0);
    });
  }

  /**
   * SVG overlay: WB H-connectors, LB H-connectors (even rounds), then orthogonal feeds.
   * No lines between WB and LB matches. Finalists→GF lines are omitted when Finals wraps below WB+LB.
   */
  private drawConnectors(): void {
    const host = this.bracketConnectorsHost?.nativeElement;
    const svg = this.connectorSvg?.nativeElement;
    if (!host || !svg) return;
    while (svg.firstChild) {
      svg.removeChild(svg.firstChild);
    }
    if (!this.bracketMatches.length) return;

    const hostRect = host.getBoundingClientRect();
    if (hostRect.width <= 0 || hostRect.height <= 0) return;

    svg.setAttribute('width', String(hostRect.width));
    svg.setAttribute('height', String(hostRect.height));
    svg.setAttribute('viewBox', `0 0 ${hostRect.width} ${hostRect.height}`);

    this.drawWbMergeConnectors(host, svg, hostRect);
    this.drawLbEvenRoundMergeConnectors(host, svg, hostRect);
    const skipGfConnectors = this.isFinalsWrappedBelowWbLb(host);
    this.drawFeedOrthogonalEdges(host, svg, hostRect, skipGfConnectors);
  }

  /**
   * When flex-wrap puts Finals on a new row under WB+LB, long WB→GF / LB→GF lines are noise — skip them.
   */
  private isFinalsWrappedBelowWbLb(host: HTMLElement): boolean {
    const wbLb = host.querySelector('.bracket-wb-lb-column') as HTMLElement | null;
    const finals = host.querySelector('.bracket-section--finals') as HTMLElement | null;
    if (!wbLb || !finals) return false;
    const a = wbLb.getBoundingClientRect();
    const b = finals.getBoundingClientRect();
    return b.top >= a.bottom - 4;
  }

  private drawWbMergeConnectors(host: HTMLElement, svg: SVGSVGElement, hostRect: DOMRect): void {
    for (let r = 1; r < this.wbColumns.length; r++) {
      for (let i = 0; i < this.wbColumns[r].length; i++) {
        const idA = `wb-${r - 1}-${2 * i}`;
        const idB = `wb-${r - 1}-${2 * i + 1}`;
        const idT = `wb-${r}-${i}`;
        this.drawMergeHConnector(host, svg, hostRect, idA, idB, idT);
      }
    }
  }

  /** LB rounds j ≥ 2 and even: two LB winners merge (same as WB). */
  private drawLbEvenRoundMergeConnectors(host: HTMLElement, svg: SVGSVGElement, hostRect: DOMRect): void {
    for (const def of this.bracketMatches) {
      if (def.segment !== 'LB' || def.round < 2 || def.round % 2 !== 0) continue;
      const j = def.round;
      const i = def.indexInRound;
      this.drawMergeHConnector(host, svg, hostRect, `lb-${j - 1}-${2 * i}`, `lb-${j - 1}-${2 * i + 1}`, def.id);
    }
  }

  /**
   * Classic H between two sources and one target. Geometry uses `.bracket-match` cards (not the
   * outer wrap that includes the game id column) so lines meet the visible boxes.
   */
  private drawMergeHConnector(
    host: HTMLElement,
    svg: SVGSVGElement,
    hostRect: DOMRect,
    idA: string,
    idB: string,
    idT: string
  ): void {
    const ra = this.connectorRelRectForMatch(host, idA, hostRect);
    const rb = this.connectorRelRectForMatch(host, idB, hostRect);
    const rt = this.connectorRelRectForMatch(host, idT, hostRect);
    if (!ra || !rb || !rt) return;

    const yA = ra.cy;
    const yB = rb.cy;
    const joinX = (Math.max(ra.right, rb.right) + rt.left) / 2;
    const midY = (yA + yB) / 2;

    const d = `M ${ra.right} ${yA} L ${joinX} ${yA} L ${joinX} ${yB} L ${rb.right} ${yB} M ${joinX} ${midY} L ${rt.left} ${midY}`;
    this.appendConnectorPath(svg, d);
  }

  /**
   * Single-feed edges (odd LB rounds, GF entry, etc.). Skips H-merge rounds and any WB↔LB link.
   * @param skipGfConnectors when Finals wraps below WB+LB, omit lines into GF (unhelpful long verticals).
   */
  private drawFeedOrthogonalEdges(
    host: HTMLElement,
    svg: SVGSVGElement,
    hostRect: DOMRect,
    skipGfConnectors: boolean
  ): void {
    for (const def of this.bracketMatches) {
      if (def.id === 'gf-1-0') continue;
      if (def.id === 'gf-0-0' && skipGfConnectors) continue;
      if (def.segment === 'WB' && def.round > 0) continue;
      if (def.segment === 'LB' && def.round >= 2 && def.round % 2 === 0) continue;
      for (const feed of [def.top, def.bottom]) {
        if ((feed.kind === 'winner' || feed.kind === 'loser') && feed.matchId) {
          if (!this.shouldDrawConnector(feed.matchId, def.id)) continue;
          this.drawOrthogonalConnector(host, svg, hostRect, feed.matchId, def.id);
        }
      }
    }
  }

  /** Allowed: same bracket (WB→WB, LB→LB) or finalist into GF (WB→GF, LB→GF). Never WB↔LB. */
  private shouldDrawConnector(sourceId: string, targetId: string): boolean {
    const s = this.connectorBracketSegment(sourceId);
    const t = this.connectorBracketSegment(targetId);
    if (s === null || t === null) return false;
    if (s === t) return true;
    if (t === 'GF' && (s === 'WB' || s === 'LB')) return true;
    return false;
  }

  private connectorBracketSegment(matchId: string): 'WB' | 'LB' | 'GF' | null {
    if (matchId.startsWith('wb-')) return 'WB';
    if (matchId.startsWith('lb-')) return 'LB';
    if (matchId.startsWith('gf-')) return 'GF';
    return null;
  }

  private drawOrthogonalConnector(
    host: HTMLElement,
    svg: SVGSVGElement,
    hostRect: DOMRect,
    sourceId: string,
    targetId: string
  ): void {
    if (!this.shouldDrawConnector(sourceId, targetId)) return;
    const rs = this.connectorRelRectForMatch(host, sourceId, hostRect);
    const rt = this.connectorRelRectForMatch(host, targetId, hostRect);
    if (!rs || !rt) return;

    const sx = rs.right;
    const sy = rs.cy;
    const tx = rt.left;
    const ty = rt.cy;

    let d: string;
    if (tx >= sx) {
      const midx = (sx + tx) / 2;
      d = `M ${sx} ${sy} L ${midx} ${sy} L ${midx} ${ty} L ${tx} ${ty}`;
    } else {
      const midy = (sy + ty) / 2;
      d = `M ${sx} ${sy} L ${sx} ${midy} L ${tx} ${midy} L ${tx} ${ty}`;
    }
    this.appendConnectorPath(svg, d);
  }

  /** Resolve `[data-bracket-match-id]` then measure `.bracket-match` for line endpoints. */
  private connectorRelRectForMatch(
    host: HTMLElement,
    matchId: string,
    hostRect: DOMRect
  ): {
    left: number;
    right: number;
    top: number;
    bottom: number;
    cy: number;
  } | null {
    const wrap = host.querySelector(`[data-bracket-match-id="${matchId}"]`) as HTMLElement | null;
    if (!wrap) return null;
    const card = wrap.querySelector('.bracket-match') as HTMLElement | null;
    const el = card ?? wrap;
    return this.connectorRelRect(el, hostRect);
  }

  private connectorRelRect(el: HTMLElement, hostRect: DOMRect): {
    left: number;
    right: number;
    top: number;
    bottom: number;
    cy: number;
  } {
    const r = el.getBoundingClientRect();
    return {
      left: r.left - hostRect.left,
      right: r.right - hostRect.left,
      top: r.top - hostRect.top,
      bottom: r.bottom - hostRect.top,
      cy: (r.top + r.bottom) / 2 - hostRect.top
    };
  }

  private appendConnectorPath(svg: SVGSVGElement, d: string): void {
    const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
    path.setAttribute('d', d);
    path.setAttribute('fill', 'none');
    path.setAttribute('stroke', 'rgba(125, 171, 240, 0.55)');
    path.setAttribute('stroke-width', '1.5');
    path.setAttribute('stroke-linecap', 'square');
    path.setAttribute('stroke-linejoin', 'miter');
    svg.appendChild(path);
  }
}
