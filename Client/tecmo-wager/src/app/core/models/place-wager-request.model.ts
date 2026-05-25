export type WagerMarketType = 'Spread' | 'OverUnder' | 'MoneyLine';
export type WagerSide = 'Player1Spread' | 'Player2Spread' | 'Over' | 'Under' | 'Player1ML' | 'Player2ML';

export interface PlaceWagerRequest {
  gameResultId: number;
  marketType: WagerMarketType;
  side: WagerSide;
  stakeAmount: number;
}
