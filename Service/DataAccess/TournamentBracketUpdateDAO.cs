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

        public async Task<IEnumerable<TournamentBracketUpdateDAOModel>> GetByTournamentIdAsync(int tournamentId, int statusId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"SELECT p.* 
                            FROM TC_TournamentBracketUpdates p                            
                            WHERE (TournamentID = @TournamentID and statusId = @statusId)";
                return await connection.QueryAsync<TournamentBracketUpdateDAOModel>(sql, new { tournamentId, statusId });
            }
        }

        public async Task<TournamentBracketUpdateDAOModel?> GetByUpdateIdAsync(int tournamentBracketUpdateId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"SELECT p.* 
                            FROM TC_TournamentBracketUpdates p                            
                            WHERE (TournamentBracketUpdateID = @tournamentBracketUpdateId)";
                return await connection.QueryFirstOrDefaultAsync<TournamentBracketUpdateDAOModel>(sql, new { tournamentBracketUpdateId });
            }
        }

        public async Task<TournamentBracketUpdateDAOModel> Save(TournamentBracketUpdateDAOModel updateModel)
        {
            if(updateModel.TournamentBracketUpdateId > 0)
                return await update(updateModel);
            else 
                return await insert(updateModel);
        }
        private async Task<TournamentBracketUpdateDAOModel> insert(TournamentBracketUpdateDAOModel update)
        {

            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"insert into TC_TournamentBracketUpdates 
                        (TournamentID, GameResultId, StatusID) 
                            values 
                        (@TournamentID, @GameResultId, @StatusID)                    

                        SELECT CAST(SCOPE_IDENTITY() as int) ";
                var id = await connection.ExecuteAsync(sql, new { update.TournamentBracketUpdateId, update.TournamentId, update.GameResultId, update.StatusID });
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
                    GameResultId = @GameResultId,
                    StatusID = @StatusID,
                    DateUpdated = getDate()
                    WHERE TournamentBracketUpdateID = @TournamentBracketUpdateID";
                await connection.ExecuteAsync(sql, new { update.TournamentBracketUpdateId, update.TournamentId, update.GameResultId, update.StatusID });
                return update;
            }
        }
    }
}
