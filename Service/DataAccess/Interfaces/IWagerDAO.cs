using TecmoTourney;
using TecmoTourney.DataAccess.Models;

namespace TecmoTourney.DataAccess.Interfaces
{
    public interface IWagerDAO
    {
        Task<WagerDAOModel> CreateAsync(WagerDAOModel wager);
        Task<WagerDAOModel?> GetByIdAsync(int wagerId);
        Task<IEnumerable<WagerDAOModel>> GetByPlayerIdAsync(int playerId, WagerStatus? statusFilter = null);
        Task<IEnumerable<WagerWithMatchupDAOModel>> GetByPlayerIdWithMatchupAsync(int playerId, int? tournamentId, WagerStatus? statusFilter = null);
        Task<IEnumerable<AdminPendingWagerRowDAOModel>> GetPendingByTournamentWithMatchupAsync(int tournamentId);
        Task<IEnumerable<WagerDAOModel>> GetByGameResultIdAsync(int gameResultId);
        Task<bool> UpdateStatusAsync(int wagerId, WagerStatus status, DateTime? cancelledAt = null, DateTime? settledAt = null);
    }
}
