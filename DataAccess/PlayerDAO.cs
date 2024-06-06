using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.DataAccess.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace TecmoTourney.DataAccess
{
    public class PlayerDAO
    {
        private readonly string _connectionString;

        public PlayerDAO(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<PlayerDAOModel>> ListPlayersAsync(int tourneyId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM Players WHERE TournamentId = @TourneyId";
                return await connection.QueryAsync<PlayerDAOModel>(sql, new { TourneyId = tourneyId });
            }
        }

        public async Task<PlayerDAOModel> GetPlayerAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM Players WHERE PlayerId = @Id";
                return await connection.QuerySingleOrDefaultAsync<PlayerDAOModel>(sql, new { Id = id });
            }
        }

        public async Task<PlayerDAOModel> AddPlayerAsync(PlayerDAOModel player)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "INSERT INTO Players (Name, ProfilePic) VALUES (@Name, @ProfilePic); SELECT CAST(SCOPE_IDENTITY() as int)";
                var id = await connection.QuerySingleAsync<int>(sql, player);
                player.PlayerId = id;
                return player;
            }
        }

        public async Task<PlayerDAOModel> UpdatePlayerAsync(int id, PlayerDAOModel player)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "UPDATE Players SET Name = @Name, ProfilePic = @ProfilePic WHERE PlayerId = @Id";
                await connection.ExecuteAsync(sql, new { player.Name, player.ProfilePic, Id = id });
                return player;
            }
        }

        public async Task<bool> DeletePlayerAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "DELETE FROM Players WHERE PlayerId = @Id";
                var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
                return rowsAffected > 0;
            }
        }
    }
}
