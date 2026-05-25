using Dapper;
using Microsoft.Data.SqlClient;
using TecmoTourney.DataAccess.Interfaces;
using TecmoTourney.DataAccess.Models;

namespace TecmoTourney.DataAccess
{
    public class WagerSettingsDAO : IWagerSettingsDAO
    {
        private readonly string _connectionString;

        public WagerSettingsDAO(ApplicationConfig config)
        {
            _connectionString = config.MainDBConnectionString;
        }

        public async Task<WagerSettingsDAOModel> GetAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT TOP 1 * FROM TC_WagerSettings ORDER BY WagerSettingsId";
            var result = await connection.QuerySingleOrDefaultAsync<WagerSettingsDAOModel>(sql);
            if (result == null)
                return new WagerSettingsDAOModel { ShowActionOnGames = true, MaxMarketImbalance = 50 };
            return result;
        }

        public async Task<bool> UpdateAsync(WagerSettingsDAOModel settings)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "UPDATE TC_WagerSettings SET ShowActionOnGames = @ShowActionOnGames, MaxMarketImbalance = @MaxMarketImbalance WHERE WagerSettingsId = @WagerSettingsId";
            var rows = await connection.ExecuteAsync(sql, settings);
            if (rows > 0)
                return true;
            // Table exists but no row (e.g. row deleted); insert default row so admin settings always work
            if (settings.WagerSettingsId <= 0)
            {
                var insertSql = @"INSERT INTO TC_WagerSettings (ShowActionOnGames, MaxMarketImbalance) VALUES (@ShowActionOnGames, @MaxMarketImbalance)";
                await connection.ExecuteAsync(insertSql, settings);
                return true;
            }
            return false;
        }
    }
}
