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

        public GameResultDAO(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<GameResultDAOModel>> ListResultsByTournamentAsync(int tourneyId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM GameResults WHERE TournamentId = @TourneyId";
                return await connection.QueryAsync<GameResultDAOModel>(sql, new { TourneyId = tourneyId });
            }
        }

        public async Task<IEnumerable<GameResultDAOModel>> ListResultsByPlayerAsync(int playerId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM GameResults WHERE Player1Id = @PlayerId OR Player2Id = @PlayerId";
                return await connection.QueryAsync<GameResultDAOModel>(sql, new { PlayerId = playerId });
            }
        }

        public async Task<IEnumerable<GameResultDAOModel>> SearchAsync(int player1Id, int player2Id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM GameResults WHERE (Player1Id = @Player1Id AND Player2Id = @Player2Id) OR (Player1Id = @Player2Id AND Player2Id = @Player1Id)";
                return await connection.QueryAsync<GameResultDAOModel>(sql, new { Player1Id = player1Id, Player2Id = player2Id });
            }
        }

        public async Task AddGameResultAsync(GameResultDAOModel gameResult)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "INSERT INTO GameResults (Player1Id, Player2Id, Score1, Score2, PassingYards1, PassingYards2, RushingYards1, RushingYards2, TournamentId) VALUES (@Player1Id, @Player2Id, @Score1, @Score2, @PassingYards1, @PassingYards2, @RushingYards1, @RushingYards2, @TournamentId)";
                await connection.ExecuteAsync(sql, gameResult);
            }
        }

        public async Task UpdateGameResultAsync(int gameResultId, GameResultDAOModel gameResult)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "UPDATE GameResults SET Player1Id = @Player1Id, Player2Id = @Player2Id, Score1 = @Score1, Score2 = @Score2, PassingYards1 = @PassingYards1, PassingYards2 = @PassingYards2, RushingYards1 = @RushingYards1, RushingYards2 = @RushingYards2, TournamentId = @TournamentId WHERE GameResultId = @GameResultId";
                await connection.ExecuteAsync(sql, new { gameResult.Player1Id, gameResult.Player2Id, gameResult.Score1, gameResult.Score2, gameResult.PassingYards1, gameResult.PassingYards2, gameResult.RushingYards1, gameResult.RushingYards2, gameResult.TournamentId, GameResultId = gameResultId });
            }
        }

        public async Task DeleteGameResultAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "DELETE FROM GameResults WHERE GameResultId = @Id";
                await connection.ExecuteAsync(sql, new { Id = id });
            }
        }
    }
}
