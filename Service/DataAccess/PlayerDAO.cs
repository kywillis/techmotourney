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

        public async Task<IEnumerable<PlayerDAOModel>> ListPlayersEligibleForGoogleLinkAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"SELECT * FROM TC_Players p
WHERE p.IsDeleted = 0
AND (p.GoogleSubjectId IS NULL OR LTRIM(RTRIM(p.GoogleSubjectId)) = '')
ORDER BY p.FullName";
                return await connection.QueryAsync<PlayerDAOModel>(sql);
            }
        }

        public async Task<bool> TryLinkGoogleAndEmailAsync(int playerId, string googleSubjectId, string emailAddress)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"UPDATE TC_Players
SET GoogleSubjectId = @GoogleSubjectId, EmailAddress = @EmailAddress
WHERE PlayerId = @PlayerId AND IsDeleted = 0
AND (GoogleSubjectId IS NULL OR LTRIM(RTRIM(GoogleSubjectId)) = '')";
                var rows = await connection.ExecuteAsync(sql, new { PlayerId = playerId, GoogleSubjectId = googleSubjectId, EmailAddress = emailAddress });
                return rows > 0;
            }
        }

        public async Task<IEnumerable<PlayerDAOModel>> ListPlayersAsync(int? tourneyId = null, bool includeDeleted = false)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var includeDeletedValue = includeDeleted ? 1 : 0;
                string sql;
                if (tourneyId.HasValue)
                {
                    sql = @$"SELECT DISTINCT p.*
                            FROM TC_Players p
                            INNER JOIN TC_PlayerTournaments pt ON p.PlayerId = pt.PlayerId
                            WHERE pt.TournamentId = @TournamentId
                            AND (p.IsDeleted = 0 OR 1 = {includeDeletedValue})";
                    return await connection.QueryAsync<PlayerDAOModel>(sql, new { TournamentId = tourneyId.Value });
                }
                sql = @$"SELECT DISTINCT p.*
                        FROM TC_Players p
                        LEFT OUTER JOIN TC_PlayerTournaments pt ON p.PlayerId = pt.PlayerId
                        WHERE (@TourneyId IS NULL OR pt.TournamentId = @TourneyId) AND
                        (p.IsDeleted = 0 OR 1 = {includeDeletedValue})";
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

        public async Task<PlayerDAOModel?> GetPlayerByGoogleSubjectIdAsync(string googleSubjectId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM TC_Players p WHERE p.GoogleSubjectId = @GoogleSubjectId and p.IsDeleted = 0";
                return await connection.QuerySingleOrDefaultAsync<PlayerDAOModel>(sql, new { GoogleSubjectId = googleSubjectId });
            }
        }

        public async Task<PlayerDAOModel> AddPlayerAsync(PlayerDAOModel player)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "INSERT INTO TC_Players (FullName, EmailAddress, ProfilePic, GoogleSubjectId, IsAdmin, Balance, IsActive) VALUES (@FullName, @EmailAddress, @ProfilePic, @GoogleSubjectId, @IsAdmin, @Balance, @IsActive); " +
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

                if(player.ProfilePic < 1)
                    sql = "UPDATE TC_Players SET FullName = @FullName, EmailAddress = @EmailAddress, ProfilePic = null WHERE PlayerId = @Id";

                await connection.ExecuteAsync(sql, new { player.FullName, player.EmailAddress, player.ProfilePic, Id = id });
                return player;
            }
        }

        public async Task<bool> UpdatePlayerBalanceAsync(int playerId, decimal newBalance)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "UPDATE TC_Players SET Balance = @Balance WHERE PlayerId = @PlayerId";
                var rowsAffected = await connection.ExecuteAsync(sql, new { Balance = newBalance, PlayerId = playerId });
                return rowsAffected > 0;
            }
        }

        public async Task<bool> SetPlayerGoogleSubjectIdAsync(int playerId, string googleSubjectId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "UPDATE TC_Players SET GoogleSubjectId = @GoogleSubjectId WHERE PlayerId = @PlayerId";
                var rowsAffected = await connection.ExecuteAsync(sql, new { GoogleSubjectId = googleSubjectId, PlayerId = playerId });
                return rowsAffected > 0;
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
