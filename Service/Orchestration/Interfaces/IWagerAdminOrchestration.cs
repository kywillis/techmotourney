using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.ResultPattern;

namespace TecmoTourney.Orchestration.Interfaces
{
    public interface IWagerAdminOrchestration
    {
        Task<Operation<List<PendingActivationModel>, ApiError>> GetPendingActivationsAsync(bool includeActivated = false);
        Task<Operation<PendingActivationModel, ApiError>> GetPendingActivationByIdAsync(int pendingActivationId);
        Task<Operation<PlayerModel, ApiError>> ActivatePendingAsync(int pendingActivationId, int adminPlayerId, string fullName, string emailAddress, int profilePic);
        Task<Operation<PlayerModel, ApiError>> LinkPendingToExistingPlayerAsync(int pendingActivationId, int adminPlayerId, int targetPlayerId);
        Task<Operation<List<AdminPlayerLinkListItemModel>, ApiError>> ListPlayersEligibleForGoogleLinkAsync();
        Task<Operation<bool, ApiError>> UpdatePlayerBalanceAsync(int adminPlayerId, WagerBalanceRequestModel request);
        Task<Operation<WagerSettingsModel, ApiError>> GetWagerSettingsAsync();
        Task<Operation<WagerSettingsModel, ApiError>> UpdateWagerSettingsAsync(WagerSettingsModel settings);
        Task<Operation<List<WagerAuditEntryModel>, ApiError>> GetAllAuditAsync(int? tournamentId = null);
        Task<Operation<List<WagerModel>, ApiError>> GetPendingWagersForTournamentAsync(int tournamentId);
        Task<Operation<bool, ApiError>> AdminCancelWagerAsync(int adminPlayerId, int wagerId);
        Task<Operation<bool, ApiError>> UpdateGameOddsByGameResultIdAsync(int gameResultId, AdminUpdateGameOddsRequestModel request);
        Task<Operation<GameResultModel, ApiError>> SaveGameResultAdminAsync(SaveGameResultRequestModel gameResult);
        Task<Operation<List<AdminPlayerBalanceListItemModel>, ApiError>> ListPlayersForBalanceAdminAsync(int tournamentId);
    }
}
