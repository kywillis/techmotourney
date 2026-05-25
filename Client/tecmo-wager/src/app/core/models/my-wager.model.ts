export type WagerStatusApi =
  | 'Pending'
  | 'Won'
  | 'Lost'
  | 'Void'
  | 'Cancelled';

export interface MyWager {
  wagerId: number;
  playerId: number;
  gameResultId: number;
  tournamentId: number;
  marketType: string;
  side: string;
  stakeAmount: number;
  status: WagerStatusApi;
  createdAt: string;
  cancelledAt: string | null;
  settledAt: string | null;
  player1Name: string;
  player2Name: string;
  /** e.g. Sinagra (spread +3) */
  pickDescription: string;
  /** Total return if the wager wins (stake + profit). */
  potentialPayout: number;
  /** Admin pending-wagers list only. */
  bettorFullName?: string;
}
