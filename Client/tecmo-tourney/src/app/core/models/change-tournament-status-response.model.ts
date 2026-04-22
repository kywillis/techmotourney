import { ITournament } from './tournament.model';
import { IOddsGenerationStatus } from './odds-generation-status.model';

export interface IChangeTournamentStatusResponse {
  tournament: ITournament;
  oddsGeneration: IOddsGenerationStatus;
}
