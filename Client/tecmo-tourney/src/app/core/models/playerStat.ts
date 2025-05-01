import { StatType } from 'src/app/enums';
import { IGameResult } from './gameResult.model';

export interface IPlayerStat {
    playerId: number;    
    playerName: string;
    statType: StatType;
    statValue: number;
    neededToPass: number;
    valuesInAvg: number[];
    games: IGameResult[];
  }