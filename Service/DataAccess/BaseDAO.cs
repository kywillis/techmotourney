using Microsoft.Data.SqlClient;

namespace TecmoTourney.DataAccess
{
    public abstract class BaseDAO
    {
        protected readonly string _connectionString;

        protected BaseDAO(ApplicationConfig config)
        {
            _connectionString = config.MainDBConnectionString;
        }

        protected SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
