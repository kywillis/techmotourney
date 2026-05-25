/**
 * Matches server BookMoney: vig on profit only, cents, half away from zero.
 * USD: $40.00 / ($30.00) for negative.
 */
export function netReturnAfterVig(
  grossReturnIncludingStake: number,
  stake: number,
  vigPercent: number
): number {
  if (grossReturnIncludingStake < 0 || stake < 0) return 0;
  if (!vigPercent || vigPercent <= 0) {
    return round2Away(grossReturnIncludingStake);
  }
  const profit = grossReturnIncludingStake - stake;
  if (profit <= 0) {
    return round2Away(grossReturnIncludingStake);
  }
  const vig = (profit * vigPercent) / 100;
  return round2Away(grossReturnIncludingStake - vig);
}

function round2Away(n: number): number {
  const s = n < 0 ? -1 : 1;
  return s * (Math.round(Math.abs(n) * 100 + 1e-8) / 100);
}

export function formatBookUsd(n: number): string {
  if (n === 0 || Object.is(n, -0)) return '$0.00';
  const abs = Math.abs(n);
  const s = abs.toFixed(2);
  if (n < 0) {
    return `($${s})`;
  }
  return `$${s}`;
}

/** House net: positive = to house, negative = to players; zero = "even" (matches server ntfy copy). */
export function formatNetSummaryLine(scope: string, net: number): string {
  if (net === 0 || Object.is(net, -0)) {
    return `${scope}: even`;
  }
  if (net > 0) {
    return `${scope}: paid to house ${formatBookUsd(net)}`;
  }
  return `${scope}: paid to players ${formatBookUsd(net)}`;
}
