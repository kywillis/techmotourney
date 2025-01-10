import { IGameResult } from "./gameResult.model";

export interface ITournamentBracketUpdate {
    tournamentBracketUpdateId: number;
    tournamentId: number;
    bracketGameId: number;
    status: string;
    gameResult: IGameResult;
    dateAdded: string;
}