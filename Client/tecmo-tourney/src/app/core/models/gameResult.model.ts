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
    bracketGameId: number | null;
  }