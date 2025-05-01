import { BracketLocation } from "src/app/enums";

export interface IPointSpread{
    player1ID: number;
    player2ID: number;
    bracketType: BracketLocation;
    spread: number;
    favoredPlayerId: number;
    summary:string;
}