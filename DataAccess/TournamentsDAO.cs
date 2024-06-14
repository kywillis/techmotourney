﻿
using Microsoft.Data.SqlClient;
using Dapper;
using TecmoTourney.DataAccess.Interfaces;
using TecmoTourney.DataAccess.Models;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;

namespace TecmoTourney.DataAccess
{
    public class TournamentsDAO : BaseDAO, ITournamentsDAO
    {
        public TournamentsDAO(ApplicationConfig config) : base(config) { } 

        public async Task<IEnumerable<TournamentDAOModel>> ListAllAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM TC_Tournaments";
                return await connection.QueryAsync<TournamentDAOModel>(sql);
            }
        }

        public async Task<PlayerModel> ListResultsByPlayerAsync(int playerId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM TC_Players WHERE PlayerId = @PlayerId";
                return await connection.QuerySingleOrDefaultAsync<PlayerModel>(sql, new { PlayerId = playerId });
            }
        }

        public async Task AddTournamentAsync(CreateTournamentRequestModel tournament)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "INSERT INTO TC_Tournaments (Name, StartDate, EndDate) VALUES (@Name, @StartDate, @EndDate)";
                await connection.ExecuteAsync(sql, tournament);
            }
        }

        public async Task UpdateTournamentAsync(int tournamentId, UpdateTournamentRequestModel tournament)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "UPDATE TC_Tournaments SET Name = @Name, StartDate = @StartDate, EndDate = @EndDate WHERE TournamentId = @TournamentId";
                await connection.ExecuteAsync(sql, new { tournament.Name, tournament.StartDate, tournament.EndDate, TournamentId = tournamentId });
            }
        }

        public async Task DeleteTournamentAsync(int tournamentId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "DELETE FROM TC_Tournaments WHERE TournamentId = @TournamentId";
                await connection.ExecuteAsync(sql, new { TournamentId = tournamentId });
            }
        }
    }
}
