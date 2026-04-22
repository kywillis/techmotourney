using TecmoTourney.DataAccess.Models;

namespace TecmoTourney.DataAccess.Interfaces
{
    public interface IGameResultSaveAuditDAO
    {
        Task InsertAsync(GameResultSaveAuditDAOModel row);
    }
}
