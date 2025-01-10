using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.DataAccess.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using TecmoTourney.DataAccess.Interfaces;

namespace TecmoTourney.DataAccess
{
    public class GameTeamDAO : BaseDAO, IGameTeamDAO
    {
        public GameTeamDAO(ApplicationConfig config) : base(config) { }

        public async Task<IEnumerable<GameTeamDAOModel>> GetAll()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"SELECT * 
                            FROM TC_GameTeams";
                return await connection.QueryAsync<GameTeamDAOModel>(sql);
            }
        }
    }
}
