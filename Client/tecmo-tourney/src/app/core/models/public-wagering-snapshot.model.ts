export interface IPublicWageringOdds {
  spread: number;
  favoredPlayerId: number | null;
  overUnder: number | null;
  moneyLinePlayer1: number | null;
  moneyLinePlayer2: number | null;
  summary: string;
}

export interface IPublicWageringMarketDepth {
  maxMarketImbalance: number;
  spreadPlayer1: number;
  spreadPlayer2: number;
  over: number;
  under: number;
  moneyLinePlayer1: number;
  moneyLinePlayer2: number;
}

export interface IPublicWageringSnapshot {
  gameResultId: number;
  player1Id: number;
  player2Id: number;
  player1Name: string;
  player2Name: string;
  player1ProfilePic: number;
  player2ProfilePic: number;
  odds: IPublicWageringOdds;
  marketDepth: IPublicWageringMarketDepth;
}
