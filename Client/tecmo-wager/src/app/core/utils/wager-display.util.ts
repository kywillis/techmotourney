/** Human-readable pick from API enum strings. */
export function formatWagerPick(
  marketType: string,
  side: string,
  player1Name: string,
  player2Name: string
): string {
  const p1 = (player1Name || '').trim() || '—';
  const p2 = (player2Name || '').trim() || '—';
  switch (side) {
    case 'Player1Spread':
      return `${p1} (spread)`;
    case 'Player2Spread':
      return `${p2} (spread)`;
    case 'Over':
      return 'Over';
    case 'Under':
      return 'Under';
    case 'Player1ML':
      return `${p1} (moneyline)`;
    case 'Player2ML':
      return `${p2} (moneyline)`;
    default:
      return side;
  }
}

export function formatMarketLabel(marketType: string): string {
  switch (marketType) {
    case 'Spread':
      return 'Spread';
    case 'OverUnder':
      return 'Over / Under';
    case 'MoneyLine':
      return 'Money line';
    default:
      return marketType;
  }
}

export function formatWagerStatus(status: string): string {
  switch (status) {
    case 'Pending':
      return 'Open';
    case 'Won':
      return 'Won';
    case 'Lost':
      return 'Lost';
    case 'Void':
      return 'Void';
    case 'Cancelled':
      return 'Cancelled';
    default:
      return status;
  }
}
