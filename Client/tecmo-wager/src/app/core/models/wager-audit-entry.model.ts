export type WagerAuditAction =
  | 'PlaceWager'
  | 'CancelWager'
  | 'SettleWagerWin'
  | 'SettleWagerLose'
  | 'VoidWager'
  | 'BalanceSet'
  | 'BalanceAdd'
  | 'BalanceSetToZero'
  | 'AdminCancelWager';

export interface WagerAuditEntry {
  auditId: number;
  tournamentId: number | null;
  targetPlayerId: number;
  actorPlayerId: number | null;
  action: string;
  wagerId: number | null;
  gameResultId: number | null;
  amount: number | null;
  balanceBefore: number | null;
  balanceAfter: number | null;
  createdAt: string;
}
