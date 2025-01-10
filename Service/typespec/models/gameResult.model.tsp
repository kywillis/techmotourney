import "./gameResultStats.model.tsp";
model GameResult{
    gameResultId: int32;
    player1: GameResultStats;
    player2: GameResultStats;
    tournamentId: int32;
}