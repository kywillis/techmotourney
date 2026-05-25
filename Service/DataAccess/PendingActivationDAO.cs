using Dapper;
using Microsoft.Data.SqlClient;
using TecmoTourney;
using TecmoTourney.DataAccess.Interfaces;
using TecmoTourney.DataAccess.Models;

namespace TecmoTourney.DataAccess
{
    public class PendingActivationDAO : IPendingActivationDAO
    {
        private readonly string _connectionString;

        public PendingActivationDAO(ApplicationConfig config)
        {
            _connectionString = config.MainDBConnectionString;
        }

        public async Task<PendingActivationDAOModel?> GetByGoogleSubjectIdAsync(string googleSubjectId)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT * FROM TC_PendingActivations WHERE GoogleSubjectId = @GoogleSubjectId";
            return await connection.QuerySingleOrDefaultAsync<PendingActivationDAOModel>(sql, new { GoogleSubjectId = googleSubjectId });
        }

        public async Task<IEnumerable<PendingActivationDAOModel>> ListAsync(bool includeActivated = false)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = includeActivated
                ? "SELECT * FROM TC_PendingActivations ORDER BY RequestedAt DESC"
                : "SELECT * FROM TC_PendingActivations WHERE Status = @Status ORDER BY RequestedAt DESC";
            return await connection.QueryAsync<PendingActivationDAOModel>(sql, new { Status = PendingActivationStatus.Pending });
        }

        public async Task<PendingActivationDAOModel?> GetByIdAsync(int pendingActivationId)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT * FROM TC_PendingActivations WHERE PendingActivationId = @Id";
            return await connection.QuerySingleOrDefaultAsync<PendingActivationDAOModel>(sql, new { Id = pendingActivationId });
        }

        public async Task<PendingActivationDAOModel> CreateAsync(PendingActivationDAOModel pending)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"INSERT INTO TC_PendingActivations (GoogleSubjectId, Email, FullName, RequestedProfilePic, Status, RequestedAt)
                        VALUES (@GoogleSubjectId, @Email, @FullName, @RequestedProfilePic, @Status, @RequestedAt);
                        SELECT CAST(SCOPE_IDENTITY() AS INT)";
            var id = await connection.ExecuteScalarAsync<int>(sql, pending);
            pending.PendingActivationId = id;
            return pending;
        }

        public async Task<bool> UpdateAsync(PendingActivationDAOModel pending)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"UPDATE TC_PendingActivations SET Status = @Status, ActivatedAt = @ActivatedAt, ActivatedByPlayerId = @ActivatedByPlayerId,
                        Email = @Email, FullName = @FullName, RequestedProfilePic = @RequestedProfilePic
                        WHERE PendingActivationId = @PendingActivationId";
            var rows = await connection.ExecuteAsync(sql, pending);
            return rows > 0;
        }
    }
}
