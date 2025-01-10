export interface ISaveTournamentRequest {
    tournamentId: number;
    name: string;
    playerIds: number[];
    bracketData: any;
  }