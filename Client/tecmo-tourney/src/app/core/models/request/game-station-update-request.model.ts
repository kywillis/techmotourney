export interface IGameStationUpdateRequest {
  startGame: boolean;
  /** When the game is in progress, set true to move it back to Waiting. */
  revertToWaiting?: boolean;
  player1GameTeamId: number;
  player2GameTeamId: number;
}
