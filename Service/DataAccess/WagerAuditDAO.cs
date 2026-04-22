using Dapper;
using Microsoft.Data.SqlClient;
using TecmoTourney.DataAccess.Interfaces;
using TecmoTourney.DataAccess.Models;

namespace TecmoTourney.DataAccess
{
    public class WagerAuditDAO : IWagerAuditDAO
    {
        private readonly string _connectionString;

        public WagerAuditDAO(ApplicationConfig config)
        {
            _connectionString = config.MainDBConnectionString;
        }

        public async Task<int> InsertAsync(WagerAuditDAOModel audit)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"INSERT INTO TC_WagerAudit (TournamentId, TargetPlayerId, ActorPlayerId, Action, WagerId, GameResultId, Amount, BalanceBefore, BalanceAfter, CreatedAt)
                        VALUES (@TournamentId, @TargetPlayerId, @ActorPlayerId, @Action, @WagerId, @GameResultId, @Amount, @BalanceBefore, @BalanceAfter, @CreatedAt);
                        SELECT CAST(SCOPE_IDENTITY() AS INT)";
            var id = await connection.ExecuteScalarAsync<int>(sql, audit);
            return id;
        }

        public async Task<IEnumerable<WagerAuditDAOModel>> GetByWagerIdAsync(int wagerId)
        {
            using var connection = new SqlConnection(_connectionString);
            const string sql = "SELECT * FROM TC_WagerAudit WHERE WagerId = @WagerId ORDER BY CreatedAt DESC, AuditId DESC";
            return await connection.QueryAsync<WagerAuditDAOModel>(sql, new { WagerId = wagerId });
        }

        public async Task<IEnumerable<WagerAuditDAOModel>> GetByTargetPlayerIdAsync(int targetPlayerId, int? tournamentId = null)
        {
            using var connection = new SqlConnection(_connectionString);
            if (tournamentId.HasValue)
            {
                var sql = "SELECT * FROM TC_WagerAudit WHERE TargetPlayerId = @TargetPlayerId AND (TournamentId = @TournamentId OR TournamentId IS NULL) ORDER BY CreatedAt DESC";
                return await connection.QueryAsync<WagerAuditDAOModel>(sql, new { TargetPlayerId = targetPlayerId, TournamentId = tournamentId });
            }
            var sqlAll = "SELECT * FROM TC_WagerAudit WHERE TargetPlayerId = @TargetPlayerId ORDER BY CreatedAt DESC";
            return await connection.QueryAsync<WagerAuditDAOModel>(sqlAll, new { TargetPlayerId = targetPlayerId });
        }

        public async Task<IEnumerable<WagerAuditDAOModel>> GetAllAsync(int? tournamentId = null)
        {
            using var connection = new SqlConnection(_connectionString);
            if (tournamentId.HasValue)
            {
                var sql = "SELECT * FROM TC_WagerAudit WHERE TournamentId = @TournamentId OR TournamentId IS NULL ORDER BY CreatedAt DESC";
                return await connection.QueryAsync<WagerAuditDAOModel>(sql, new { TournamentId = tournamentId });
            }
            var sqlAll = "SELECT * FROM TC_WagerAudit ORDER BY CreatedAt DESC";
            return await connection.QueryAsync<WagerAuditDAOModel>(sqlAll);
        }
    }
}
