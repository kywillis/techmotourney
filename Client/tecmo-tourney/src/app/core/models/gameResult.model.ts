import { GameStatus, GameType } from 'src/app/enums';
import {IGameResultPlayer} from './gameResultPlayer.model';

export interface IGameResult {
    gameResultId: number;    
    tournamentId: number;
    player1: IGameResultPlayer;
    player2: IGameResultPlayer;
    date: Date;
    status: GameStatus;
    gameType: GameType;
    /** If set, this game does not count toward this player's preliminary seeding. */
    seedingExemptPlayerId?: number | null;
    /** UTC when the match was marked in progress at the game station. */
    gameStartedAt?: string | null;
  }