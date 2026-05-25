using TecmoTourney.DataAccess.Models;

namespace TecmoTourney.DataAccess.Interfaces
{
    public interface IPendingActivationDAO
    {
        Task<PendingActivationDAOModel?> GetByGoogleSubjectIdAsync(string googleSubjectId);
        Task<IEnumerable<PendingActivationDAOModel>> ListAsync(bool includeActivated = false);
        Task<PendingActivationDAOModel?> GetByIdAsync(int pendingActivationId);
        Task<PendingActivationDAOModel> CreateAsync(PendingActivationDAOModel pending);
        Task<bool> UpdateAsync(PendingActivationDAOModel pending);
    }
}
