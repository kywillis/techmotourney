import { BracketLocation } from "src/app/enums";

/** Point-spread line for a tournament game, keyed by gameResultId in the bracket viewer. */
export interface IBracketOddsLine {
  spread: number;
  favoredPlayerId: number | null;
}

/** Aligns with API `GameOddsModel` (camelCase Player1Id / Player2Id). */
export interface IPointSpread {
    gameResultId?: number | null;
    player1Id: number;
    player2Id: number;
    /** Legacy / iframe payloads may still use this casing; prefer `player1Id`. */
    player1ID?: number;
    player2ID?: number;
    bracketType: BracketLocation;
    spread: number;
    favoredPlayerId: number;
    summary?: string;
    moneyLinePlayer1?: number | null;
    moneyLinePlayer2?: number | null;
    overUnder?: number | null;
}