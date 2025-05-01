import { IMatchUpResult } from "./matchupResult.model";
import { IPlayer } from "./player.model";

export interface IOpponentMatchUpResult extends IMatchUpResult{
    playerId: number;
    opponentId: number;
    opponentName: string;
}