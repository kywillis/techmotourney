import { GameStatus } from 'src/app/enums';

export type FeedKind = 'seed' | 'bye' | 'winner' | 'loser' | 'empty';

export interface FeedRef {
  kind: FeedKind;
  /** WB/LB match id, e.g. wb-0-1 or lb-2-0 */
  matchId?: string;
  /** 0-based seed index in ordered entrant list (best seed = 0) */
  seedSlot?: number;
}

export interface BracketMatch {
  id: string;
  segment: 'WB' | 'LB' | 'GF';
  /** Round within segment (GF: 0 = first final, 1 = reset) */
  round: number;
  indexInRound: number;
  top: FeedRef;
  bottom: FeedRef;
}

export interface BracketParticipant {
  playerId: number;
  name: string;
  seed: number;
}

export interface ResolvedSlot {
  participant: BracketParticipant | null;
  /** Bye placeholder */
  isBye?: boolean;
}

export interface ResolvedMatch {
  def: BracketMatch;
  top: ResolvedSlot;
  bottom: ResolvedSlot;
  gameResultId: number | null;
  status: GameStatus | null;
  topScore: number | null;
  bottomScore: number | null;
  /** Predicted winner playerId for Completed games */
  winnerId: number | null;
  /** Optional: both slots known but no game row yet */
  isPending: boolean;
  /**
   * Losers bracket only: winners-bracket game # whose loser feeds this slot (e.g. "WB2").
   * Numbering is WB1..WBn in bracket order — round 1 left→right, then round 2, etc.
   */
  topSourceLabel?: string | null;
  bottomSourceLabel?: string | null;
  /** Winners bracket: single label for the matchup (same WB# as losers bracket references). */
  wbMatchLabel?: string | null;
}
