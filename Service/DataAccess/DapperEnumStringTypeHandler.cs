using System.Data;
using Dapper;
using TecmoTourney;

namespace TecmoTourney.DataAccess
{
    /// <summary>
    /// Dapper type handler for enums stored as string (NVARCHAR) in SQL Server.
    /// Converts enum to string when writing and parses string to enum when reading.
    /// </summary>
    public sealed class DapperEnumStringTypeHandler<T> : SqlMapper.TypeHandler<T> where T : struct, Enum
    {
        public override T Parse(object value)
        {
            if (value == null || value is DBNull)
                return default;

            var s = value.ToString();
            if (string.IsNullOrEmpty(s))
                return default;

            return Enum.TryParse<T>(s, true, out var result) ? result : default;
        }

        public override void SetValue(IDbDataParameter parameter, T value)
        {
            parameter.Value = value.ToString();
            parameter.DbType = DbType.String;
        }
    }

    /// <summary>
    /// Registers Dapper type handlers for wager-related enums stored as NVARCHAR in the database.
    /// <see cref="WagerStatus"/> (TC_Wagers) and <see cref="WagerAuditAction"/> (TC_WagerAudit) use INT; no handler.
    /// Call once at application startup (e.g. from Program.cs).
    /// </summary>
    public static class WagerDapperRegistration
    {
        public static void RegisterWagerEnumHandlers()
        {
            SqlMapper.AddTypeHandler(new DapperEnumStringTypeHandler<PendingActivationStatus>());
            SqlMapper.AddTypeHandler(new DapperEnumStringTypeHandler<WagerMarketType>());
            SqlMapper.AddTypeHandler(new DapperEnumStringTypeHandler<WagerSide>());
        }
    }
}
