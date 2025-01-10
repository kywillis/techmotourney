import "../gameResultStats.model.tsp";
model CreateGameResultRequest{
    player1: GameResultStats;
    player2: GameResultStats;
    tournamentId: int32;
}