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

        public async Task<IEnumerable<PlayerDAOModel>> ListPlayersAsync(int? tourneyId = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"SELECT p.* 
                            FROM TC_Players p
                            Left Outer JOIN TC_PlayerTournaments pt ON p.PlayerId = pt.PlayerId
                            WHERE (@TourneyId is null or pt.TournamentId = @TourneyId) and p.IsDeleted = 0";
                return await connection.QueryAsync<PlayerDAOModel>(sql, new { TourneyId = tourneyId });
            }
        }

        public async Task<PlayerDAOModel?> GetPlayerAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM TC_Players p WHERE p.PlayerId = @Id and p.IsDeleted = 0";
                return await connection.QuerySingleOrDefaultAsync<PlayerDAOModel>(sql, new { Id = id });
            }
        }

        public async Task<PlayerDAOModel> AddPlayerAsync(PlayerDAOModel player)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "INSERT INTO TC_Players (FullName, EmailAddress, ProfilePic) VALUES (@FullName, @EmailAddress, @ProfilePic); " +
                    "SELECT CAST(SCOPE_IDENTITY() as int)";
                var id = await connection.QuerySingleAsync<int>(sql, player);
                player.PlayerId = id;
                return player;
            }
        }

        public async Task<PlayerDAOModel> UpdatePlayerAsync(int id, PlayerDAOModel player)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "UPDATE TC_Players SET FullName = @FullName, EmailAddress = @EmailAddress, ProfilePic = @ProfilePic WHERE PlayerId = @Id";

                if(string.IsNullOrEmpty(player.ProfilePic))
                    sql = "UPDATE TC_Players SET FullName = @FullName, EmailAddress = @EmailAddress, ProfilePic = null WHERE PlayerId = @Id";

                await connection.ExecuteAsync(sql, new { player.FullName, player.EmailAddress, player.ProfilePic, Id = id });
                return player;
            }
        }

        public async Task<bool> DeletePlayerAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "Update TC_Players set IsDeleted = 1 WHERE PlayerId = @Id";
                var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
                return rowsAffected > 0;
            }
        }
    }
}
