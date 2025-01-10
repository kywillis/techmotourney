import { TournamentStatus } from 'src/app/enums';
import { ISaveGameResultRequest } from './saveGameResultRequest.model';

export interface IChangeTournamentStatusRequest {
    status: TournamentStatus;
    tournamentId: number;
    newGames: ISaveGameResultRequest[];
    bracketData: any;
}