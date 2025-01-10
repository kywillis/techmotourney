using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using Dapper;
using TecmoTourney.DataAccess.Interfaces;
using TecmoTourney.DataAccess.Models;

namespace TecmoTourney.DataAccess
{
    public class GameResultDAO : IGameResultDAO
    {
        private readonly string _connectionString;

        public GameResultDAO(ApplicationConfig config)
        {
            _connectionString = config.MainDBConnectionString;
        }

        public async Task<IEnumerable<GameResultDAOModel>> ListResultsByTournamentAsync(int tournamentId, bool includeDeledted)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM TC_GameResults WHERE TournamentId = @tournamentId and (isDeleted = 0 or @includeDeledted = 1)";
                return await connection.QueryAsync<GameResultDAOModel>(sql, new { tournamentId = tournamentId, includeDeledted });
            }
        }

        public async Task<IEnumerable<GameResultDAOModel>> ListResultsByPlayerAsync(int playerId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM TC_GameResults WHERE Player1Id = @PlayerId OR Player2Id = @PlayerId and isDeleted = 0";
                return await connection.QueryAsync<GameResultDAOModel>(sql, new { PlayerId = playerId });
            }
        }

        public async Task<IEnumerable<GameResultDAOModel>> ListResultsByBracketGameIDsAsync(IEnumerable<int> bracketGameIds)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM TC_GameResults WHERE bracketGameId IN @bracketGameIds";
                return await connection.QueryAsync<GameResultDAOModel>(sql, new { bracketGameIds });
            }
        }


        public async Task<IEnumerable<GameResultDAOModel>> SearchAsync(int tournamentId, int? player1Id, int? player2Id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"SELECT * FROM TC_GameResults WHERE TournamentId = @TournamentId and isDeleted = 0";

                if (player1Id.HasValue)
                {
                    sql += " AND (Player1Id = @Player1Id OR Player2Id = @Player1Id)";
                }

                if (player2Id.HasValue)
                {
                    sql += " AND (Player1Id = @Player2Id OR Player2Id = @Player2Id)";
                }

                var parameters = new { TournamentId = tournamentId, Player1Id = player1Id, Player2Id = player2Id };

                return await connection.QueryAsync<GameResultDAOModel>(sql, parameters);
            }
        }

        public async Task<GameResultDAOModel> CreateGameResultAsync(GameResultDAOModel gameResult)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"INSERT INTO TC_GameResults (Player1Id, Player2Id, Player1Score, Player2Score, Player1PassingYards, Player2PassingYards, Player1RushingYards, Player2RushingYards, TournamentId, Player1GameTeamID, Player2GameTeamID, StatusId, GameTypeId, IsDeleted, BracketGameId) 
                            VALUES (@Player1Id, @Player2Id, @Player1Score, @Player2Score, @Player1PassingYards, @Player2PassingYards, @Player1RushingYards, @Player2RushingYards, @TournamentId, @Player1GameTeamID, @Player2GameTeamID, @StatusId, @GameTypeId, @IsDeleted, @BracketGameId); 
                            SELECT CAST(SCOPE_IDENTITY() as int)";
                var id = await connection.ExecuteScalarAsync<int>(sql, gameResult);
                return (await GetGameResultAsync(id))!;
            }
        }

        public async Task<GameResultDAOModel?> GetGameResultAsync(int gameResultId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM TC_GameResults WHERE GameResultId = @GameResultId";
                return (await connection.QueryAsync<GameResultDAOModel>(sql, new { GameResultId = gameResultId })).FirstOrDefault();
            }
        }

        public async Task UpdateGameResultAsync(int gameResultId, GameResultDAOModel gameResult)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"UPDATE TC_GameResults 
                            SET Player1Id = @Player1Id, Player2Id = @Player2Id, Player1Score = @Player1Score, Player2Score = @Player2Score, Player1PassingYards = @Player1PassingYards, Player2PassingYards = @Player2PassingYards, 
                                Player1RushingYards = @Player1RushingYards, Player2RushingYards = @Player2RushingYards, TournamentId = @TournamentId, Player1GameTeamID = @Player1GameTeamID, Player2GameTeamID = @Player2GameTeamID, 
                                GameTypeId = @GameTypeId, StatusId = @StatusId
                            WHERE GameResultId = @GameResultId";
                await connection.ExecuteAsync(sql, new
                {
                    gameResult.Player1Id,
                    gameResult.Player2Id,
                    gameResult.Player1Score,
                    gameResult.Player2Score,
                    gameResult.Player1PassingYards,
                    gameResult.Player2PassingYards,
                    gameResult.Player1RushingYards,
                    gameResult.Player2RushingYards,
                    gameResult.TournamentId,
                    gameResult.Player1GameTeamID,
                    gameResult.Player2GameTeamID,
                    gameResult.GameTypeId,
                    gameResult.StatusId,
                    GameResultId = gameResultId
                });
            }
        }

        public async Task DeleteGameResultAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "UPDATE TC_GameResults SET IsDeleted = 1 WHERE GameResultId = @Id";
                await connection.ExecuteAsync(sql, new { Id = id });
            }
        }

        public async Task UnDeleteGameResultAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "UPDATE TC_GameResults SET IsDeleted = 0 WHERE GameResultId = @Id";
                await connection.ExecuteAsync(sql, new { Id = id });
            }
        }
    }
}
