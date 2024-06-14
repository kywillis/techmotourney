using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using TecmoTourney.DataAccess.Models;
using TecmoTourney.DataAccess.Interfaces;

namespace TecmoTourney.DataAccess
{
    public class PlayerTournamentDAO : BaseDAO, IPlayerTournamentDAO
    {
        public PlayerTournamentDAO(ApplicationConfig config) : base(config) { }

        public async Task<int> CreatePlayerTournamentAsync(PlayerTournamentDAOModel playerTournament)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "INSERT INTO TC_PlayerTournaments (PlayerId, TournamentId, DateAdded, DateModified) " +
                          "VALUES (@PlayerId, @TournamentId, GETDATE(), GETDATE()); " +
                          "SELECT CAST(SCOPE_IDENTITY() as int)";
                return await connection.QuerySingleAsync<int>(sql, playerTournament);
            }
        }

        public async Task<IEnumerable<PlayerTournamentDAOModel>> GetByTournamentIdAsync(int tournamentId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM TC_PlayerTournaments WHERE TournamentId = @TournamentId";
                return await connection.QueryAsync<PlayerTournamentDAOModel>(sql, new { TournamentId = tournamentId });
            }
        }

        public async Task<IEnumerable<PlayerTournamentDAOModel>> GetByPlayerIdAsync(int playerId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM TC_PlayerTournaments WHERE PlayerId = @PlayerId";
                return await connection.QueryAsync<PlayerTournamentDAOModel>(sql, new { PlayerId = playerId });
            }
        }

        public async Task DeleteByPlayerAndTournamentIdAsync(int playerId, int tournamentId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "DELETE FROM TC_PlayerTournaments WHERE PlayerId = @PlayerId AND TournamentId = @TournamentId";
                await connection.ExecuteAsync(sql, new { PlayerId = playerId, TournamentId = tournamentId });
            }
        }
    }
}
