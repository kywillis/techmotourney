using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.DataAccess.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using TecmoTourney.DataAccess.Interfaces;

namespace TecmoTourney.DataAccess
{
    public class PlayerDAO : BaseDAO, IPlayerDAO
    {
        public PlayerDAO(ApplicationConfig config) : base(config) { }

        public async Task<IEnumerable<PlayerDAOModel>> ListPlayersAsync(int tourneyId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM TC_Players WHERE TournamentId = @TourneyId";
                return await connection.QueryAsync<PlayerDAOModel>(sql, new { TourneyId = tourneyId });
            }
        }

        public async Task<PlayerDAOModel> GetPlayerAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM TC_Players WHERE PlayerId = @Id";
                return await connection.QuerySingleOrDefaultAsync<PlayerDAOModel>(sql, new { Id = id });
            }
        }

        public async Task<PlayerDAOModel> AddPlayerAsync(PlayerDAOModel player)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "INSERT INTO TC_Players (FullName, ProfilePic) VALUES (@FullName, @ProfilePic); SELECT CAST(SCOPE_IDENTITY() as int)";
                var id = await connection.QuerySingleAsync<int>(sql, player);
                player.PlayerId = id;
                return player;
            }
        }

        public async Task<PlayerDAOModel> UpdatePlayerAsync(int id, PlayerDAOModel player)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "UPDATE TC_Players SET FullName = @FullName, ProfilePic = @ProfilePic WHERE PlayerId = @Id";
                await connection.ExecuteAsync(sql, new { player.FullName, player.ProfilePic, Id = id });
                return player;
            }
        }

        public async Task<bool> DeletePlayerAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "DELETE FROM TC_Players WHERE PlayerId = @Id";
                var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
                return rowsAffected > 0;
            }
        }
    }
}
