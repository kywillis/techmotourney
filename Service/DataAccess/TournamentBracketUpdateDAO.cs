using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.DataAccess.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using TecmoTourney.DataAccess.Interfaces;

namespace TecmoTourney.DataAccess
{
    public class TournamentBracketUpdateDAO : BaseDAO, ITournamentBracketUpdateDAO
    {
        public TournamentBracketUpdateDAO(ApplicationConfig config) : base(config) { }

        public async Task<IEnumerable<TournamentBracketUpdateDAOModel>> GetByTournamentIdAsync(int tournamentId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"SELECT p.* 
                            FROM TC_TournamentBracketUpdates p                            
                            WHERE (TournamentID = @TournamentID)";
                return await connection.QueryAsync<TournamentBracketUpdateDAOModel>(sql, new { tournamentId });
            }
        }

        public async Task<TournamentBracketUpdateDAOModel> Save(TournamentBracketUpdateDAOModel updateModel)
        {
            if(updateModel.TournamentBracketUpdateId > 1)
                return await update(updateModel);
            else 
                return await insert(updateModel);
        }
        private async Task<TournamentBracketUpdateDAOModel> insert(TournamentBracketUpdateDAOModel update)
        {

            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"insert into TC_TournamentBracketUpdates 
                        (TournamentID, BracketGameID, StatusID) 
                            values 
                        (@TournamentID, @BracketGameID, @StatusID)                    

                        SELECT CAST(SCOPE_IDENTITY() as int) ";
                var id = await connection.QuerySingleAsync<int>(sql, new { update.TournamentBracketUpdateId, update.TournamentId, update.BracketGameId, update.StatusID });
                update.TournamentBracketUpdateId = id;
                return update;
            }
        }
        private async Task<TournamentBracketUpdateDAOModel> update(TournamentBracketUpdateDAOModel update)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"Update TC_TournamentBracketUpdates set 
                    TournamentID = @TournamentID,
                    BracketGameID = @BracketGameID,
                    StatusID = @StatusID,
                    DateUpdated = getDate(),
                    WHERE TournamentBracketUpdateID = @TournamentBracketUpdateID";
                await connection.QuerySingleAsync<int>(sql, new { update.TournamentBracketUpdateId, update.TournamentId, update.BracketGameId, update.StatusID });
                return update;
            }
        }
    }
}
