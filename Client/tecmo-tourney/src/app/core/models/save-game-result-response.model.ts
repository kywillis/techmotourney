import { IGameResult } from './gameResult.model';
import { IOddsGenerationStatus } from './odds-generation-status.model';
import { IRecalculateBracketResponse } from './recalculate-bracket-response.model';

export interface ISaveGameResultResponse {
  gameResult: IGameResult;
  oddsGeneration: IOddsGenerationStatus;
  bracketReconciliation?: IRecalculateBracketResponse | null;
}
