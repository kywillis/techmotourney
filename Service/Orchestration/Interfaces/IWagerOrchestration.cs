using TecmoTourney;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.ResultPattern;

namespace TecmoTourney.Orchestration.Interfaces
{
    public interface IWagerOrchestration
    {
        Task<Operation<decimal, ApiError>> GetBalanceAsync(int playerId);
        Task<Operation<List<WagerModel>, ApiError>> GetMyWagersAsync(int playerId, WagerStatus? statusFilter = null, int? tournamentId = null);
        Task<Operation<List<WagerAuditEntryModel>, ApiError>> GetMyAuditAsync(int playerId, int? tournamentId = null);
        Task<Operation<decimal?, ApiError>> GetFinalBalanceForTournamentAsync(int playerId, int tournamentId);
        Task<Operation<List<BettableGameModel>, ApiError>> GetGamesAvailableToBetAsync();
        Task<Operation<WagerGamesBoardModel, ApiError>> GetGamesBoardAsync();
        Task<Operation<BettableGameModel, ApiError>> GetGameDetailForWagerAsync(int gameResultId);
        /// <summary>Same payload as bettor game detail, but allows any game state (admin lines/scores UI).</summary>
        Task<Operation<BettableGameModel, ApiError>> GetGameDetailForAdminAsync(int gameResultId);
        /// <summary>Odds, optional summary, and pending stake totals for any non-deleted game with lines. No auth.</summary>
        Task<Operation<PublicWageringSnapshotModel, ApiError>> GetPublicWageringSnapshotAsync(int gameResultId);
        /// <summary>Same as single-game snapshot for each game in the tournament that has odds. No auth.</summary>
        Task<Operation<List<PublicWageringSnapshotModel>, ApiError>> GetPublicWageringSnapshotsByTournamentAsync(int tournamentId);
        Task<Operation<TournamentSummaryModel, ApiError>> GetTournamentSummaryForUserAsync(int playerId, int tournamentId);
        Task<Operation<WagerModel, ApiError>> PlaceWagerAsync(int playerId, PlaceWagerRequestModel request);
    }
}
