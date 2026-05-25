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

        /// <summary>House net for settled wagers on this game: stakes kept from losses minus win payouts (positive = house up).</summary>
        Task<decimal> GetSettledWagerNetForGameResultAsync(int gameResultId);

        /// <summary>Same net across all non-cancelled settled wagers in the tournament.</summary>
        Task<decimal> GetSettledWagerNetForTournamentAsync(int tournamentId);

        /// <summary>Latest SettleWager win payout per winning wager for this game (for ntfy lines).</summary>
        Task<IReadOnlyDictionary<int, decimal>> GetWinPayoutsByWagerIdForGameResultAsync(int gameResultId);

        /// <summary>Per-bettor settled P&amp;L for the tournament: win credits minus lost stakes; void counts as 0.</summary>
        Task<IReadOnlyList<PlayerTournamentPnlRowDAOModel>> GetPlayerSettledPnlByTournamentAsync(int tournamentId);

        Task<IReadOnlyList<PendingStakeByPlayerRowDAOModel>> GetPendingStakeByPlayerForTournamentAsync(int tournamentId);
        Task<IReadOnlyList<PendingStakeByGameRowDAOModel>> GetPendingStakeByGameForTournamentAsync(int tournamentId);
        Task<(decimal StakeTotal, int WagerCount)> GetTournamentPendingStakeSummaryAsync(int tournamentId);
        Task<IReadOnlyList<int>> GetDistinctGameResultIdsWithWagersForTournamentAsync(int tournamentId);
        Task<IEnumerable<WagerWithMatchupDAOModel>> GetWagersWithMatchupByGameResultIdAsync(int gameResultId);
    }
}
