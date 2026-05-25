import { BettableGame } from './bettable-game.model';

export interface WagerGamesBoard {
  openForBetting: BettableGame[];
  inProgress: BettableGame[];
  completed: BettableGame[];
}
