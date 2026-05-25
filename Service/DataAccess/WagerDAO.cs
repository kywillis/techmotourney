using Dapper;
using Microsoft.Data.SqlClient;
using TecmoTourney;
using TecmoTourney.DataAccess.Interfaces;
using TecmoTourney.DataAccess.Models;

namespace TecmoTourney.DataAccess
{
    public class WagerDAO : IWagerDAO
    {
        private readonly string _connectionString;

        public WagerDAO(ApplicationConfig config)
        {
            _connectionString = config.MainDBConnectionString;
        }

        public async Task<WagerDAOModel> CreateAsync(WagerDAOModel wager)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"INSERT INTO TC_Wagers (PlayerId, GameResultId, TournamentId, MarketType, Side, StakeAmount, Status, CreatedAt)
                        VALUES (@PlayerId, @GameResultId, @TournamentId, @MarketType, @Side, @StakeAmount, @Status, @CreatedAt);
                        SELECT CAST(SCOPE_IDENTITY() AS INT)";
            var id = await connection.ExecuteScalarAsync<int>(sql, wager);
            wager.WagerId = id;
            return wager;
        }

        public async Task<WagerDAOModel?> GetByIdAsync(int wagerId)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT * FROM TC_Wagers WHERE WagerId = @WagerId";
            return await connection.QuerySingleOrDefaultAsync<WagerDAOModel>(sql, new { WagerId = wagerId });
        }

        public async Task<IEnumerable<WagerDAOModel>> GetByPlayerIdAsync(int playerId, WagerStatus? statusFilter = null)
        {
            using var connection = new SqlConnection(_connectionString);
            if (!statusFilter.HasValue)
            {
                var sql = "SELECT * FROM TC_Wagers WHERE PlayerId = @PlayerId ORDER BY CreatedAt DESC";
                return await connection.QueryAsync<WagerDAOModel>(sql, new { PlayerId = playerId });
            }
            var sqlFiltered = "SELECT * FROM TC_Wagers WHERE PlayerId = @PlayerId AND Status = @Status ORDER BY CreatedAt DESC";
            return await connection.QueryAsync<WagerDAOModel>(sqlFiltered, new { PlayerId = playerId, Status = statusFilter.Value });
        }

        public async Task<IEnumerable<WagerWithMatchupDAOModel>> GetByPlayerIdWithMatchupAsync(
            int playerId,
            int? tournamentId,
            WagerStatus? statusFilter = null)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
SELECT w.WagerId, w.PlayerId, w.GameResultId, w.TournamentId, w.MarketType, w.Side, w.StakeAmount, w.Status, w.CreatedAt, w.CancelledAt, w.SettledAt,
       COALESCE(NULLIF(LTRIM(RTRIM(p1.FullName)), ''), CONCAT('Player ', gr.Player1Id)) AS Player1Name,
       COALESCE(NULLIF(LTRIM(RTRIM(p2.FullName)), ''), CONCAT('Player ', gr.Player2Id)) AS Player2Name,
       gr.Player1Id AS MatchPlayer1Id,
       gr.Player2Id AS MatchPlayer2Id,
       ISNULL(odds.Spread, 0) AS OddsSpread,
       odds.FavoredPlayerId AS OddsFavoredPlayerId,
       odds.MoneyLinePlayer1 AS OddsMoneyLinePlayer1,
       odds.MoneyLinePlayer2 AS OddsMoneyLinePlayer2,
       odds.OverUnder AS OddsOverUnder
FROM TC_Wagers w
INNER JOIN TC_GameResults gr ON w.GameResultId = gr.GameResultId AND gr.IsDeleted = 0
LEFT JOIN TC_Players p1 ON gr.Player1Id = p1.PlayerId AND ISNULL(p1.IsDeleted, 0) = 0
LEFT JOIN TC_Players p2 ON gr.Player2Id = p2.PlayerId AND ISNULL(p2.IsDeleted, 0) = 0
OUTER APPLY (
    SELECT TOP 1 o.Spread, o.FavoredPlayerId, o.MoneyLinePlayer1, o.MoneyLinePlayer2, o.OverUnder
    FROM TC_GameOdds o
    WHERE o.GameResultId = w.GameResultId
) odds
WHERE w.PlayerId = @PlayerId
  AND (@TournamentId IS NULL OR w.TournamentId = @TournamentId)
  AND (@Status IS NULL OR w.Status = @Status)
ORDER BY w.CreatedAt DESC";
            return await connection.QueryAsync<WagerWithMatchupDAOModel>(sql, new
            {
                PlayerId = playerId,
                TournamentId = tournamentId,
                Status = statusFilter
            });
        }

        public async Task<IEnumerable<AdminPendingWagerRowDAOModel>> GetPendingByTournamentWithMatchupAsync(int tournamentId)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
SELECT w.WagerId, w.PlayerId, w.GameResultId, w.TournamentId, w.MarketType, w.Side, w.StakeAmount, w.Status, w.CreatedAt, w.CancelledAt, w.SettledAt,
       COALESCE(NULLIF(LTRIM(RTRIM(p1.FullName)), ''), CONCAT('Player ', gr.Player1Id)) AS Player1Name,
       COALESCE(NULLIF(LTRIM(RTRIM(p2.FullName)), ''), CONCAT('Player ', gr.Player2Id)) AS Player2Name,
       gr.Player1Id AS MatchPlayer1Id,
       gr.Player2Id AS MatchPlayer2Id,
       ISNULL(odds.Spread, 0) AS OddsSpread,
       odds.FavoredPlayerId AS OddsFavoredPlayerId,
       odds.MoneyLinePlayer1 AS OddsMoneyLinePlayer1,
       odds.MoneyLinePlayer2 AS OddsMoneyLinePlayer2,
       odds.OverUnder AS OddsOverUnder,
       COALESCE(NULLIF(LTRIM(RTRIM(bp.FullName)), ''), CONCAT('Player ', w.PlayerId)) AS BettorFullName
FROM TC_Wagers w
INNER JOIN TC_GameResults gr ON w.GameResultId = gr.GameResultId AND gr.IsDeleted = 0
LEFT JOIN TC_Players p1 ON gr.Player1Id = p1.PlayerId AND ISNULL(p1.IsDeleted, 0) = 0
LEFT JOIN TC_Players p2 ON gr.Player2Id = p2.PlayerId AND ISNULL(p2.IsDeleted, 0) = 0
LEFT JOIN TC_Players bp ON w.PlayerId = bp.PlayerId AND ISNULL(bp.IsDeleted, 0) = 0
OUTER APPLY (
    SELECT TOP 1 o.Spread, o.FavoredPlayerId, o.MoneyLinePlayer1, o.MoneyLinePlayer2, o.OverUnder
    FROM TC_GameOdds o
    WHERE o.GameResultId = w.GameResultId
) odds
WHERE w.TournamentId = @TournamentId AND w.Status = @PendingStatus
ORDER BY w.CreatedAt DESC";
            return await connection.QueryAsync<AdminPendingWagerRowDAOModel>(sql, new
            {
                TournamentId = tournamentId,
                PendingStatus = WagerStatus.Pending
            });
        }

        public async Task<IEnumerable<WagerDAOModel>> GetByGameResultIdAsync(int gameResultId)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT * FROM TC_Wagers WHERE GameResultId = @GameResultId";
            return await connection.QueryAsync<WagerDAOModel>(sql, new { GameResultId = gameResultId });
        }

        public async Task<bool> UpdateStatusAsync(int wagerId, WagerStatus status, DateTime? cancelledAt = null, DateTime? settledAt = null)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "UPDATE TC_Wagers SET Status = @Status, CancelledAt = @CancelledAt, SettledAt = @SettledAt WHERE WagerId = @WagerId";
            var rows = await connection.ExecuteAsync(sql, new { WagerId = wagerId, Status = status, CancelledAt = cancelledAt, SettledAt = settledAt });
            return rows > 0;
        }
    }
}
