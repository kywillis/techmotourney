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
                // Check if the player is already in the tournament
                var checkSql = "SELECT COUNT(*) FROM TC_PlayerTournaments WHERE PlayerId = @PlayerId AND TournamentId = @TournamentId";
                var count = await connection.QuerySingleAsync<int>(checkSql, new { playerTournament.PlayerId, playerTournament.TournamentId });

                if (count > 0)
                {
                    throw new InvalidOperationException("Player is already in the tournament.");
                }

                var sql = "INSERT INTO TC_PlayerTournaments (PlayerId, TournamentId, DateAdded) " +
                          "VALUES (@PlayerId, @TournamentId, GETDATE()); " +
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
