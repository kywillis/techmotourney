import { IOddsGenerationStatus } from './odds-generation-status.model';

export interface IRecalculateBracketResponse {
  createdGameResultIds: number[];
  softDeletedGameResultIds: number[];
  oddsGeneration: IOddsGenerationStatus;
  skipped: boolean;
  skipReason?: string | null;
}
