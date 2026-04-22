using TecmoTourney.DataAccess.Models;

namespace TecmoTourney.DataAccess.Interfaces
{
    public interface IWagerAuditDAO
    {
        Task<int> InsertAsync(WagerAuditDAOModel audit);
        Task<IEnumerable<WagerAuditDAOModel>> GetByWagerIdAsync(int wagerId);
        Task<IEnumerable<WagerAuditDAOModel>> GetByTargetPlayerIdAsync(int targetPlayerId, int? tournamentId = null);
        Task<IEnumerable<WagerAuditDAOModel>> GetAllAsync(int? tournamentId = null);
    }
}
