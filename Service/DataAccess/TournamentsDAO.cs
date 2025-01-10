
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

        public async Task<TournamentDAOModel> AddTournamentAsync(UpdateTournamentRequestModel tournament)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "INSERT INTO TC_Tournaments (Name, StartDate, EndDate, StatusId) VALUES (@Name, @StartDate, @EndDate, @StatusId)" +
                            "SELECT CAST(SCOPE_IDENTITY() as int)";
                var id = await connection.QuerySingleAsync<int>(sql, tournament);
                return (await GetById(id))! ;
            }
        }
        public async Task<TournamentDAOModel?> GetById(int tournamentId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM TC_Tournaments where tournamentId = @tournamentId";
                return await connection.QuerySingleOrDefaultAsync<TournamentDAOModel>(sql, new { tournamentId });
            }
        }

        public async Task<IEnumerable<TournamentDAOModel>> ListAllAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM TC_Tournaments";
                return await connection.QueryAsync<TournamentDAOModel>(sql);
            }
        }

        public async Task<PlayerModel?> ListResultsByPlayerAsync(int playerId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "SELECT * FROM TC_Players WHERE PlayerId = @PlayerId";
                return await connection.QuerySingleOrDefaultAsync<PlayerModel>(sql, new { PlayerId = playerId });
            }
        }


        public async Task<TournamentDAOModel> UpdateTournamentAsync(int tournamentId, UpdateTournamentRequestModel tournament)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "UPDATE TC_Tournaments SET Name = @Name, StartDate = @StartDate, EndDate = @EndDate, BracketData = @BracketData WHERE TournamentId = @TournamentId";
                await connection.ExecuteAsync(sql, new { tournament.Name, TournamentId = tournamentId, tournament.StartDate, tournament.EndDate, tournament.BracketData });
                return (await GetById(tournamentId))!;
            }
        }

        public async Task UpdateTournamentStatusAsync(int tournamentId, int statusId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "UPDATE TC_Tournaments SET statusId = @statusId WHERE TournamentId = @TournamentId";
                await connection.ExecuteAsync(sql, new { TournamentId = tournamentId, statusId });
                return;
            }
        }
        public async Task UpdateTournamentBracketDataAsync(int tournamentId, string bracketData)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "UPDATE TC_Tournaments SET bracketData = @bracketData WHERE TournamentId = @TournamentId";
                await connection.ExecuteAsync(sql, new { TournamentId = tournamentId, bracketData });
                return;
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
