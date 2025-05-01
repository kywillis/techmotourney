using Dapper;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using TecmoTourney.DataAccess.Interfaces;
using TecmoTourney.DataAccess.Models;
using TecmoTourney.Orchestration;

namespace TecmoTourney.DataAccess
{
    public class PointSpreadDAO : IPointSpreadDAO
    {
        private readonly string _connectionString;

        public PointSpreadDAO(ApplicationConfig config)
        {
            _connectionString = config.MainDBConnectionString;
        }
        public async Task<PointSpreadDAOModel> CreatePointSpreadsAsync(PointSpreadDAOModel pointSpread)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"INSERT INTO TC_PointSpreads (Player1Id, Player2Id, TournamentId, Spread, FavoredPlayerId, BracketTypeId, summary) 
                            VALUES (@Player1Id, @Player2Id, @TournamentId, @Spread, @FavoredPlayerId, @BracketTypeId, @summary); 
                            SELECT CAST(SCOPE_IDENTITY() as int)";
                pointSpread.PointSpreadId = await connection.ExecuteScalarAsync<int>(sql, pointSpread);
                return pointSpread;
            }
        }

        public async Task<IEnumerable<PointSpreadDAOModel>> GetByTournamentIdAsync(int tournamentId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"select * from TC_PointSpreads where TournamentId = @TournamentId";
                return await connection.QueryAsync<PointSpreadDAOModel>(sql, new { TournamentId = tournamentId });
            }
        }

        public async Task DeleteByTournamentIdAsync(int tournamentId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"delete from TC_PointSpreads where TournamentId = @TournamentId";
                await connection.ExecuteAsync(sql, new {tournamentId });
            }
        }
    }
}
