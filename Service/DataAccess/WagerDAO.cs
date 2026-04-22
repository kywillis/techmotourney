using System.Collections.Generic;
using System.Linq;
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
       COALESCE(NULLIF(LTRIM(RTRIM(p1.FullName)), ''), CASE WHEN gr.GameResultId IS NOT NULL THEN CONCAT('Player ', gr.Player1Id) ELSE '' END) AS Player1Name,
       COALESCE(NULLIF(LTRIM(RTRIM(p2.FullName)), ''), CASE WHEN gr.GameResultId IS NOT NULL THEN CONCAT('Player ', gr.Player2Id) ELSE '' END) AS Player2Name,
       ISNULL(gr.Player1Id, 0) AS MatchPlayer1Id,
       ISNULL(gr.Player2Id, 0) AS MatchPlayer2Id,
       ISNULL(odds.Spread, 0) AS OddsSpread,
       odds.FavoredPlayerId AS OddsFavoredPlayerId,
       odds.MoneyLinePlayer1 AS OddsMoneyLinePlayer1,
       odds.MoneyLinePlayer2 AS OddsMoneyLinePlayer2,
       odds.OverUnder AS OddsOverUnder
FROM TC_Wagers w
LEFT JOIN TC_GameResults gr ON w.GameResultId = gr.GameResultId AND gr.IsDeleted = 0
LEFT JOIN TC_Players p1 ON gr.Player1Id = p1.PlayerId AND ISNULL(p1.IsDeleted, 0) = 0
LEFT JOIN TC_Players p2 ON gr.Player2Id = p2.PlayerId AND ISNULL(p2.IsDeleted, 0) = 0
OUTER APPLY (
    SELECT TOP 1 o.Spread, o.FavoredPlayerId, o.MoneyLinePlayer1, o.MoneyLinePlayer2, o.OverUnder
    FROM TC_GameOdds o
    WHERE w.GameResultId IS NOT NULL AND o.GameResultId = w.GameResultId AND ISNULL(o.IsDeleted, 0) = 0
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
    WHERE o.GameResultId = w.GameResultId AND ISNULL(o.IsDeleted, 0) = 0
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

        public async Task<IEnumerable<WagerDAOModel>> GetByTournamentIdAsync(int tournamentId)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT * FROM TC_Wagers WHERE TournamentId = @TournamentId";
            return await connection.QueryAsync<WagerDAOModel>(sql, new { TournamentId = tournamentId });
        }

        public async Task<bool> UpdateStatusAsync(int wagerId, WagerStatus status, DateTime? cancelledAt = null, DateTime? settledAt = null)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "UPDATE TC_Wagers SET Status = @Status, CancelledAt = @CancelledAt, SettledAt = @SettledAt WHERE WagerId = @WagerId";
            var rows = await connection.ExecuteAsync(sql, new { WagerId = wagerId, Status = status, CancelledAt = cancelledAt, SettledAt = settledAt });
            return rows > 0;
        }

        public async Task<bool> CancelPendingAndClearGameResultAsync(int wagerId, DateTime cancelledAt)
        {
            using var connection = new SqlConnection(_connectionString);
            const string sql = @"UPDATE TC_Wagers SET Status = @Cancelled, CancelledAt = @CancelledAt, GameResultId = NULL
WHERE WagerId = @WagerId AND Status = @Pending";
            var rows = await connection.ExecuteAsync(sql, new
            {
                WagerId = wagerId,
                Cancelled = WagerStatus.Cancelled,
                CancelledAt = cancelledAt,
                Pending = WagerStatus.Pending
            });
            return rows > 0;
        }

        public async Task<bool> CancelPendingKeepingGameResultAsync(int wagerId, DateTime cancelledAt)
        {
            using var connection = new SqlConnection(_connectionString);
            const string sql = @"UPDATE TC_Wagers SET Status = @Cancelled, CancelledAt = @CancelledAt
WHERE WagerId = @WagerId AND Status = @Pending";
            var rows = await connection.ExecuteAsync(sql, new
            {
                WagerId = wagerId,
                Cancelled = WagerStatus.Cancelled,
                CancelledAt = cancelledAt,
                Pending = WagerStatus.Pending
            });
            return rows > 0;
        }

        public async Task<bool> ClearGameResultIdForNonPendingAsync(int wagerId)
        {
            using var connection = new SqlConnection(_connectionString);
            const string sql = "UPDATE TC_Wagers SET GameResultId = NULL WHERE WagerId = @WagerId AND Status <> @Pending";
            var rows = await connection.ExecuteAsync(sql, new { WagerId = wagerId, Pending = WagerStatus.Pending });
            return rows > 0;
        }

        public async Task<decimal> GetSettledWagerNetForGameResultAsync(int gameResultId)
        {
            const string sql = @"
SELECT
  ISNULL((
    SELECT SUM(w.StakeAmount)
    FROM TC_Wagers w
    WHERE w.GameResultId = @GameResultId AND w.Status = 'Lost'
  ), 0)
- ISNULL((
  SELECT SUM(x.Amt)
  FROM TC_Wagers w
  CROSS APPLY (
    SELECT TOP 1 a.Amount AS Amt
    FROM TC_WagerAudit a
    WHERE a.WagerId = w.WagerId
      AND a.Action = 'SettleWagerWin'
      AND a.GameResultId = @GameResultId
    ORDER BY a.CreatedAt DESC, a.AuditId DESC
  ) x
  WHERE w.GameResultId = @GameResultId AND w.Status = 'Won'
), 0);";

            using var connection = new SqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<decimal>(sql, new { GameResultId = gameResultId });
        }

        public async Task<decimal> GetSettledWagerNetForTournamentAsync(int tournamentId)
        {
            const string sql = @"
SELECT
  ISNULL((
    SELECT SUM(w.StakeAmount)
    FROM TC_Wagers w
    WHERE w.TournamentId = @TournamentId AND w.Status = 'Lost'
  ), 0)
- ISNULL((
  SELECT SUM(x.Amt)
  FROM TC_Wagers w
  CROSS APPLY (
    SELECT TOP 1 a.Amount AS Amt
    FROM TC_WagerAudit a
    WHERE a.WagerId = w.WagerId
      AND a.Action = 'SettleWagerWin'
      AND a.GameResultId = w.GameResultId
    ORDER BY a.CreatedAt DESC, a.AuditId DESC
  ) x
  WHERE w.TournamentId = @TournamentId AND w.Status = 'Won'
), 0);";

            using var connection = new SqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<decimal>(sql, new { TournamentId = tournamentId });
        }

        public async Task<IReadOnlyDictionary<int, decimal>> GetWinPayoutsByWagerIdForGameResultAsync(int gameResultId)
        {
            const string sql = @"
SELECT w.WagerId, x.Amt AS Payout
FROM TC_Wagers w
CROSS APPLY (
  SELECT TOP 1 a.Amount AS Amt
  FROM TC_WagerAudit a
  WHERE a.WagerId = w.WagerId
    AND a.Action = 'SettleWagerWin'
    AND a.GameResultId = @GameResultId
  ORDER BY a.CreatedAt DESC, a.AuditId DESC
) x
WHERE w.GameResultId = @GameResultId AND w.Status = 'Won'";

            using var connection = new SqlConnection(_connectionString);
            var rows = await connection.QueryAsync<WinPayoutRow>(sql, new { GameResultId = gameResultId });
            return rows.ToDictionary(r => r.WagerId, r => r.Payout);
        }

        private sealed class WinPayoutRow
        {
            public int WagerId { get; set; }
            public decimal Payout { get; set; }
        }
    }
}
