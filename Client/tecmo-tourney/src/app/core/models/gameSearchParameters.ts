import { BracketLocation } from "src/app/enums";

export interface IGameSearchParameters {
    matchupLocation: BracketLocation | null;
    player1ID: number | null;
    player2ID: number | null;
    tournamentId: number | null;
}