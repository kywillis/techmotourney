import { GameStatus, GameType } from 'src/app/enums';
import {IGameResultPlayer} from 'src/app/core/models/gameResultPlayer.model';

export interface ISaveGameResultRequest {
  gameResultId?: number;
  player1: IGameResultPlayer;
  player2: IGameResultPlayer;
  tournamentId: number;
  status: GameStatus;
  gameType: GameType;
  bracketGameId: number | null;
}