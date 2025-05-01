import { IGameResult } from "./gameResult.model";

export interface ITournamentBracketUpdate {
    tournamentBracketUpdateId: number;
    gameResult: IGameResult;
}