export interface BettableGameOdds {
  spread: number;
  favoredPlayerId: number | null;
  overUnder: number | null;
  moneyLinePlayer1: number | null;
  moneyLinePlayer2: number | null;
  /** Present when the house posted an LLM/analyst blurb. */
  summary?: string;
}

/** Pending dollars per side; matches API marketDepth (camelCase). */
export interface BettableGameMarketDepth {
  maxMarketImbalance: number;
  spreadPlayer1: number;
  spreadPlayer2: number;
  over: number;
  under: number;
  moneyLinePlayer1: number;
  moneyLinePlayer2: number;
}

export interface WagerActionItem {
  playerName: string;
  side: string;
  stakeAmount: number;
}

export interface BettableGame {
  gameResultId: number;
  tournamentId: number;
  player1Id: number;
  player2Id: number;
  player1Name: string;
  player2Name: string;
  player1ProfilePic: number;
  player2ProfilePic: number;
  /** Server GameStatus name (Waiting, InProgress, Completed). */
  gameStatus?: string;
  /** True when API allows new wagers on this game. */
  isOpenForBetting?: boolean;
  odds: BettableGameOdds;
  gameStartedAt: string | null;
  /** Set for completed games from the games board API */
  player1Score?: number | null;
  player2Score?: number | null;
  marketDepth?: BettableGameMarketDepth;
  action?: WagerActionItem[];
}
