import type { BettableGameMarketDepth } from '../models/bettable-game.model';
import type { WagerMarketType, WagerSide } from '../models/place-wager-request.model';

function getMyAndOtherTotals(
  depth: BettableGameMarketDepth,
  market: WagerMarketType,
  side: WagerSide
): { my: number; other: number } {
  switch (market) {
    case 'Spread':
      if (side === 'Player1Spread') {
        return { my: depth.spreadPlayer1, other: depth.spreadPlayer2 };
      }
      return { my: depth.spreadPlayer2, other: depth.spreadPlayer1 };
    case 'OverUnder':
      if (side === 'Over') {
        return { my: depth.over, other: depth.under };
      }
      return { my: depth.under, other: depth.over };
    case 'MoneyLine':
      if (side === 'Player1ML') {
        return { my: depth.moneyLinePlayer1, other: depth.moneyLinePlayer2 };
      }
      return { my: depth.moneyLinePlayer2, other: depth.moneyLinePlayer1 };
    default:
      return { my: 0, other: 0 };
  }
}

/** Mirrors WagerOrchestration.PlaceWagerAsync imbalance check. */
export function wouldViolateMarketImbalance(
  depth: BettableGameMarketDepth | undefined | null,
  market: WagerMarketType,
  side: WagerSide,
  stake: number
): boolean {
  if (!depth || !Number.isFinite(stake) || stake <= 0) return false;
  const M = Number(depth.maxMarketImbalance);
  if (!Number.isFinite(M)) return false;
  const { my, other } = getMyAndOtherTotals(depth, market, side);
  const totalMySide = my + stake;
  const diff = Math.abs(totalMySide - other);
  return diff > M + 1e-9;
}

/**
 * Largest whole-dollar stake on this side that keeps |my+S−other| ≤ M, capped by houseMax and balance floor.
 * Returns 0 if no positive integer works.
 */
export function maxStakeForImbalance(
  depth: BettableGameMarketDepth | undefined | null,
  market: WagerMarketType,
  side: WagerSide,
  houseMax: number,
  balanceFloor: number
): number {
  if (!depth) return Math.min(houseMax, balanceFloor);
  const M = Number(depth.maxMarketImbalance);
  if (!Number.isFinite(M)) return Math.min(houseMax, balanceFloor);
  const { my, other } = getMyAndOtherTotals(depth, market, side);
  // |my + S - other| <= M  =>  S in [ -M - my + other,  M - my + other ]
  const ub = M - my + other;
  const lb = -M - my + other;
  const capByBook = Math.min(houseMax, balanceFloor, Math.floor(ub));
  const minNeed = Math.max(1, Math.ceil(lb));
  if (capByBook < minNeed) return 0;
  return capByBook;
}

/**
 * Max stake allowed by the book rule alone (|my+S−other| ≤ M), ignoring house caps and wallet.
 * Large sentinel when depth / M missing. 0 when the book window is infeasible (matches API pause).
 */
export function bookStakeUpperBound(
  depth: BettableGameMarketDepth | undefined | null,
  market: WagerMarketType,
  side: WagerSide
): number {
  if (!depth) return Number.MAX_SAFE_INTEGER;
  const M = Number(depth.maxMarketImbalance);
  if (!Number.isFinite(M)) return Number.MAX_SAFE_INTEGER;
  const { my, other } = getMyAndOtherTotals(depth, market, side);
  const ub = M - my + other;
  const lb = -M - my + other;
  const capBookOnly = Math.floor(ub);
  const minNeed = Math.max(1, Math.ceil(lb));
  if (capBookOnly < minNeed) return 0;
  return Math.max(0, capBookOnly);
}
