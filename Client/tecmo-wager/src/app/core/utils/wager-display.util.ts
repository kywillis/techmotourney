/** Game result status tone (matches tecmo-tourney game-status-badge colors). */
export type GameStatusTone = 'waiting' | 'in-progress' | 'completed' | 'unknown';

export function parseGameStatusTone(status: string | undefined): GameStatusTone {
  const s = (status || '').trim().toLowerCase().replace(/\s+/g, '');
  if (s === 'waiting') return 'waiting';
  if (s === 'inprogress') return 'in-progress';
  if (s === 'completed') return 'completed';
  return 'unknown';
}

/** Display label for API GameStatus (Waiting, InProgress, Completed). */
export function formatGameStatusLabel(status: string | undefined): string {
  switch (parseGameStatusTone(status)) {
    case 'waiting':
      return 'Waiting';
    case 'in-progress':
      return 'In progress';
    case 'completed':
      return 'Completed';
    default:
      return (status || '').trim() || '—';
  }
}

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
      return 'Wager Won';
    case 'Lost':
      return 'Wager Lost';
    case 'Void':
      return 'Void';
    case 'Cancelled':
      return 'Cancelled';
    default:
      return status;
  }
}
