/**
 * Profit on a winning wager (excluding the returned stake) at American odds.
 * +200 → $100 risk wins $200 profit; -200 → $200 risk wins $100 profit.
 */
export function profitFromAmericanOdds(stake: number, line: number): number {
  if (!Number.isFinite(stake) || stake <= 0) return 0;
  if (!Number.isFinite(line) || line === 0) return stake;
  if (line > 0) return stake * (line / 100);
  return stake * (100 / Math.abs(line));
}

/**
 * Largest whole-dollar stake whose win (profit) does not exceed maxWin.
 * +200 → floor(40*100/200)=20 to win $40; +300 → $13 to win $39; -200 → $80 to win $40.
 */
export function maxStakeForAmericanOddsWinCap(maxWin: number, line: number): number {
  if (!Number.isFinite(maxWin) || maxWin <= 0) return 0;
  if (!Number.isFinite(line) || line === 0) {
    return Math.floor(maxWin);
  }
  if (line > 0) {
    return Math.floor((maxWin * 100) / line);
  }
  return Math.floor((maxWin * Math.abs(line)) / 100);
}
