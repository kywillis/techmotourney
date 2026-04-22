using System.Collections.Generic;
using System.Linq;
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
                var sql = @"INSERT INTO TC_GameOdds (Player1Id, Player2Id, TournamentId, Spread, FavoredPlayerId, BracketTypeId, Summary, MoneyLinePlayer1, MoneyLinePlayer2, OverUnder, GameResultId, IsDeleted) 
                            VALUES (@Player1Id, @Player2Id, @TournamentId, @Spread, @FavoredPlayerId, @BracketTypeId, @Summary, @MoneyLinePlayer1, @MoneyLinePlayer2, @OverUnder, @GameResultId, @IsDeleted); 
                            SELECT CAST(SCOPE_IDENTITY() as int)";
                gameOdds.GameOddsId = await connection.ExecuteScalarAsync<int>(sql, gameOdds);
                return gameOdds;
            }
        }

        public async Task<IEnumerable<GameOddsDAOModel>> GetByTournamentIdAsync(int tournamentId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"SELECT * FROM TC_GameOdds WHERE TournamentId = @TournamentId AND ISNULL(IsDeleted, 0) = 0";
                return await connection.QueryAsync<GameOddsDAOModel>(sql, new { TournamentId = tournamentId });
            }
        }

        public async Task<GameOddsDAOModel?> GetByGameResultIdAsync(int gameResultId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"SELECT * FROM TC_GameOdds WHERE GameResultId = @GameResultId AND ISNULL(IsDeleted, 0) = 0";
                return await connection.QuerySingleOrDefaultAsync<GameOddsDAOModel>(sql, new { GameResultId = gameResultId });
            }
        }

        /// <summary>Admin line edit: does not update <c>Summary</c> (LLM text is immutable after set).</summary>
        public async Task<int> UpdateByGameResultIdAsync(
            int gameResultId,
            decimal spread,
            int? favoredPlayerId,
            decimal? moneyLinePlayer1,
            decimal? moneyLinePlayer2,
            decimal? overUnder)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"UPDATE TC_GameOdds
                        SET Spread = @Spread, FavoredPlayerId = @FavoredPlayerId, MoneyLinePlayer1 = @MoneyLinePlayer1,
                            MoneyLinePlayer2 = @MoneyLinePlayer2, OverUnder = @OverUnder, DateModified = GETUTCDATE()
                        WHERE GameResultId = @GameResultId AND ISNULL(IsDeleted, 0) = 0";
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

        public async Task<int> DeleteByGameResultIdAsync(int gameResultId)
        {
            if (gameResultId < 1)
                return 0;
            using var connection = new SqlConnection(_connectionString);
            const string sql = @"DELETE FROM TC_GameOdds WHERE GameResultId = @GameResultId";
            return await connection.ExecuteAsync(sql, new { GameResultId = gameResultId });
        }

        public async Task<int> DeleteByGameResultIdsAsync(IEnumerable<int> gameResultIds)
        {
            var ids = gameResultIds?.Where(id => id > 0).Distinct().ToArray() ?? [];
            if (ids.Length == 0)
                return 0;
            using var connection = new SqlConnection(_connectionString);
            const string sql = @"DELETE FROM TC_GameOdds WHERE GameResultId IN @Ids";
            return await connection.ExecuteAsync(sql, new { Ids = ids });
        }

        public async Task<int> SoftDeleteByGameResultIdAsync(int gameResultId)
        {
            if (gameResultId < 1)
                return 0;
            using var connection = new SqlConnection(_connectionString);
            const string sql = @"UPDATE TC_GameOdds SET IsDeleted = 1, DateModified = GETUTCDATE() WHERE GameResultId = @GameResultId AND ISNULL(IsDeleted, 0) = 0";
            return await connection.ExecuteAsync(sql, new { GameResultId = gameResultId });
        }

        public async Task<int> SoftDeleteByGameResultIdsAsync(IEnumerable<int> gameResultIds)
        {
            var ids = gameResultIds?.Where(id => id > 0).Distinct().ToArray() ?? [];
            if (ids.Length == 0)
                return 0;
            using var connection = new SqlConnection(_connectionString);
            const string sql = @"UPDATE TC_GameOdds SET IsDeleted = 1, DateModified = GETUTCDATE() WHERE GameResultId IN @Ids AND ISNULL(IsDeleted, 0) = 0";
            return await connection.ExecuteAsync(sql, new { Ids = ids });
        }
    }
}
