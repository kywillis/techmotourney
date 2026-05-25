/** From GET wager/tournament/{id}/summary — netAmount is last balance-after in audit scope, not profit. */
export interface TournamentSummary {
  tournamentId: number;
  tournamentName: string;
  wins: number;
  losses: number;
  netAmount: number;
}
