import { TournamentStatus } from 'src/app/enums';

export interface ITournament {
    tournamentId: number;
    name: string;
    location: string;
    status: TournamentStatus;
    startDate: string;
    endDate: string;
    tournamentBracket: any;
  }