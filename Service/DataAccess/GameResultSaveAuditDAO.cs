using Dapper;
using Microsoft.Data.SqlClient;
using TecmoTourney;
using TecmoTourney.DataAccess.Interfaces;
using TecmoTourney.DataAccess.Models;

namespace TecmoTourney.DataAccess
{
    public class GameResultSaveAuditDAO : IGameResultSaveAuditDAO
    {
        private readonly string _connectionString;

        public GameResultSaveAuditDAO(ApplicationConfig config)
        {
            _connectionString = config.MainDBConnectionString;
        }

        public async Task InsertAsync(GameResultSaveAuditDAOModel row)
        {
            using var connection = new SqlConnection(_connectionString);
            const string sql = @"INSERT INTO dbo.TC_GameResultSaveAudit
                (GameResultId, SaveSource, ClientCorrelationId, IsTieGame, AccumulatedStats, RequestJson, CreatedAtUtc)
                VALUES (@GameResultId, @SaveSource, @ClientCorrelationId, @IsTieGame, @AccumulatedStats, @RequestJson, @CreatedAtUtc)";
            if (row.CreatedAtUtc == default)
                row.CreatedAtUtc = DateTime.UtcNow;
            await connection.ExecuteAsync(sql, row);
        }
    }
}
