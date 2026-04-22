using System.Collections.Generic;
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
        Task<IEnumerable<WagerDAOModel>> GetByTournamentIdAsync(int tournamentId);
        Task<bool> UpdateStatusAsync(int wagerId, WagerStatus status, DateTime? cancelledAt = null, DateTime? settledAt = null);
        /// <summary>Pending only: cancel, clear GameResultId.</summary>
        Task<bool> CancelPendingAndClearGameResultAsync(int wagerId, DateTime cancelledAt);
        /// <summary>Pending only: cancel and refund; keep GameResultId for audit/history.</summary>
        Task<bool> CancelPendingKeepingGameResultAsync(int wagerId, DateTime cancelledAt);
        /// <summary>Non-pending only: set GameResultId NULL (historical row kept).</summary>
        Task<bool> ClearGameResultIdForNonPendingAsync(int wagerId);
    }
}
