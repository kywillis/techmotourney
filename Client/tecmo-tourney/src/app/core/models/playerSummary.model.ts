import { IPlayer } from "./player.model";

export interface IPlayerSummary extends IPlayer {
    wins: number;
    loses: number;
    tournamentIds: number[];
  }