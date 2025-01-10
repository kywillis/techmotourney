using TecmoTourney.DataAccess.Models;

namespace TecmoTourney
{
    public static class GameUtils
    {
        /// <summary>
        /// Checks if the given player is in this game, null if not in, 1 if they are player 1, 2 if they are player 2
        /// </summary>
        /// <param name="game"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        public static int? PlayerInGame(GameResultDAOModel game, int playerId) 
        {
            if(game.Player1Id == playerId)
                return 1;
            else if (game.Player2Id == playerId)
                return 2;
            else 
                return null;
        }

        public static int GetPlayerStat(IEnumerable<GameResultDAOModel> games, int playerId, GameStat stat)
        {
            return games.Sum(g => GetPlayerStat(g, playerId, stat));
        }

        public static int GetPlayerStat(GameResultDAOModel game, int playerId, GameStat stat)
        {
            if(PlayerInGame(game, playerId) == null)
                return 0;

            bool isPlayer1 = playerId == game.Player1Id;
            bool isPlayer2 = playerId == game.Player2Id;
            int passingYards = 0;
            int rushingYards = 0;

            switch (stat)
            {
                case GameStat.PassingYards:
                    return isPlayer1 ? game.Player1PassingYards : game.Player2PassingYards;
                case GameStat.RushingYards:
                    return isPlayer1 ? game.Player1RushingYards : game.Player2RushingYards;
                case GameStat.TotalYards:
                    passingYards = isPlayer1 ? game.Player1PassingYards : game.Player2PassingYards;
                    rushingYards = isPlayer1 ? game.Player1RushingYards : game.Player2RushingYards;
                    return passingYards + rushingYards;
                case GameStat.PassingYardsAllowed:
                    return isPlayer1 ? game.Player2PassingYards : game.Player1PassingYards;
                case GameStat.RushingYardsAllowed:
                    return isPlayer1 ? game.Player2RushingYards : game.Player1RushingYards;
                case GameStat.TotalYardsAllowed:
                    passingYards = isPlayer1 ? game.Player2PassingYards : game.Player1PassingYards;
                    rushingYards = isPlayer1 ? game.Player2RushingYards : game.Player1RushingYards;
                    return passingYards + rushingYards;
                case GameStat.PointsScoreFor:
                    return isPlayer1 ? game.Player1Score : game.Player2Score;
                case GameStat.PointsScoreAgainst:
                    return isPlayer1 ? game.Player2Score : game.Player1Score;
                case GameStat.Wins:
                    if (isPlayer1)
                        return game.Player1Score > game.Player2Score ? 1 : 0;
                    else
                        return game.Player2Score > game.Player1Score ? 1 : 0;
                case GameStat.Losses:
                    if (isPlayer1)
                        return game.Player1Score < game.Player2Score ? 1 : 0;
                    else
                        return game.Player2Score < game.Player1Score ? 1 : 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stat), "Invalid stat type.");
            }
        }
    }
}
