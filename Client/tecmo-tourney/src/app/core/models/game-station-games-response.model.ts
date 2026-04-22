import { IGameResult } from './gameResult.model';

export interface IGameStationGamesResponse {
  tournamentId: number;
  tournamentName: string;
  waiting: IGameResult[];
  inProgress: IGameResult[];
}
