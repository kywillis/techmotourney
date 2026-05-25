using Dapper;
using Microsoft.Data.SqlClient;
using TecmoTourney.DataAccess.Interfaces;
using TecmoTourney.DataAccess.Models;
using TecmoTourney.Orchestration;

namespace TecmoTourney.DataAccess
{
    public class GameOddsDAO : IGameOddsDAO
    {
        private readonly string _connectionString;

        public GameOddsDAO(ApplicationConfig config)
        {
            _connectionString = config.MainDBConnectionString;
        }

        public async Task<GameOddsDAOModel> CreatePointSpreadsAsync(GameOddsDAOModel gameOdds)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"INSERT INTO TC_GameOdds (Player1Id, Player2Id, TournamentId, Spread, FavoredPlayerId, BracketTypeId, Summary, MoneyLinePlayer1, MoneyLinePlayer2, OverUnder, GameResultId) 
                            VALUES (@Player1Id, @Player2Id, @TournamentId, @Spread, @FavoredPlayerId, @BracketTypeId, @Summary, @MoneyLinePlayer1, @MoneyLinePlayer2, @OverUnder, @GameResultId); 
                            SELECT CAST(SCOPE_IDENTITY() as int)";
                gameOdds.GameOddsId = await connection.ExecuteScalarAsync<int>(sql, gameOdds);
                return gameOdds;
            }
        }

        public async Task<IEnumerable<GameOddsDAOModel>> GetByTournamentIdAsync(int tournamentId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"SELECT * FROM TC_GameOdds WHERE TournamentId = @TournamentId";
                return await connection.QueryAsync<GameOddsDAOModel>(sql, new { TournamentId = tournamentId });
            }
        }

        public async Task<GameOddsDAOModel?> GetByGameResultIdAsync(int gameResultId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"SELECT * FROM TC_GameOdds WHERE GameResultId = @GameResultId";
                return await connection.QuerySingleOrDefaultAsync<GameOddsDAOModel>(sql, new { GameResultId = gameResultId });
            }
        }

        public async Task<int> UpdateByGameResultIdAsync(
            int gameResultId,
            int spread,
            int? favoredPlayerId,
            int? moneyLinePlayer1,
            int? moneyLinePlayer2,
            decimal? overUnder)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"UPDATE TC_GameOdds
                        SET Spread = @Spread, FavoredPlayerId = @FavoredPlayerId, MoneyLinePlayer1 = @MoneyLinePlayer1,
                            MoneyLinePlayer2 = @MoneyLinePlayer2, OverUnder = @OverUnder, DateModified = GETUTCDATE()
                        WHERE GameResultId = @GameResultId";
            return await connection.ExecuteAsync(sql, new
            {
                GameResultId = gameResultId,
                Spread = spread,
                FavoredPlayerId = favoredPlayerId,
                MoneyLinePlayer1 = moneyLinePlayer1,
                MoneyLinePlayer2 = moneyLinePlayer2,
                OverUnder = overUnder
            });
        }

        public async Task DeleteByTournamentIdAsync(int tournamentId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"DELETE FROM TC_GameOdds WHERE TournamentId = @TournamentId";
                await connection.ExecuteAsync(sql, new { tournamentId });
            }
        }
    }
}
