using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecmoTourney.Middleware;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.Orchestration.Interfaces;
using TecmoTourney.ResultPattern;

namespace TecmoTourney.Controllers
{
    [ApiController]
    [Route("api/wager/admin")]
    [Authorize(AuthenticationSchemes = "Google")]
    public class WagerAdminController : ControllerBase
    {
        private readonly IWagerAdminOrchestration _wagerAdminOrchestration;
        private readonly IWagerOrchestration _wagerOrchestration;

        public WagerAdminController(
            IWagerAdminOrchestration wagerAdminOrchestration,
            IWagerOrchestration wagerOrchestration)
        {
            _wagerAdminOrchestration = wagerAdminOrchestration;
            _wagerOrchestration = wagerOrchestration;
        }

        private int GetCurrentPlayerId()
        {
            var id = HttpContext.GetWagerPlayerId();
            if (id == null)
                throw new InvalidOperationException("WagerPlayerId not set.");
            return id.Value;
        }

        private IActionResult? RequireAdmin()
        {
            if (!HttpContext.GetWagerIsAdmin())
                return Forbid();
            return null;
        }

        [HttpGet("pending-activations")]
        [ProducesResponseType(200, Type = typeof(List<PendingActivationModel>))]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetPendingActivations([FromQuery] bool includeActivated = false)
        {
            if (RequireAdmin() is { } err) return err;
            var result = await _wagerAdminOrchestration.GetPendingActivationsAsync(includeActivated);
            return result.ToActionResult();
        }

        [HttpGet("pending-activations/{pendingActivationId}")]
        [ProducesResponseType(200, Type = typeof(PendingActivationModel))]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPendingActivationById(int pendingActivationId)
        {
            if (RequireAdmin() is { } err) return err;
            var result = await _wagerAdminOrchestration.GetPendingActivationByIdAsync(pendingActivationId);
            return result.ToActionResult();
        }

        [HttpPost("pending-activations/{pendingActivationId}/activate")]
        [ProducesResponseType(200, Type = typeof(PlayerModel))]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ActivatePending(
            int pendingActivationId,
            [FromBody] ActivatePendingRequestModel body)
        {
            if (RequireAdmin() is { } err) return err;
            var result = await _wagerAdminOrchestration.ActivatePendingAsync(
                pendingActivationId,
                GetCurrentPlayerId(),
                body.FullName,
                body.EmailAddress,
                body.ProfilePic);
            return result.ToActionResult();
        }

        [HttpGet("players/eligible-google-link")]
        [ProducesResponseType(200, Type = typeof(List<AdminPlayerLinkListItemModel>))]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetPlayersEligibleForGoogleLink()
        {
            if (RequireAdmin() is { } err) return err;
            var result = await _wagerAdminOrchestration.ListPlayersEligibleForGoogleLinkAsync();
            return result.ToActionResult();
        }

        [HttpPost("pending-activations/{pendingActivationId}/link-to-player")]
        [ProducesResponseType(200, Type = typeof(PlayerModel))]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> LinkPendingToPlayer(
            int pendingActivationId,
            [FromBody] LinkPendingToPlayerRequestModel body)
        {
            if (RequireAdmin() is { } err) return err;
            if (body.PlayerId < 1)
                return BadRequest("playerId is required");
            var result = await _wagerAdminOrchestration.LinkPendingToExistingPlayerAsync(
                pendingActivationId,
                GetCurrentPlayerId(),
                body.PlayerId);
            return result.ToActionResult();
        }

        [HttpPatch("balance")]
        [ProducesResponseType(200, Type = typeof(bool))]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdatePlayerBalance([FromBody] WagerBalanceRequestModel request)
        {
            if (RequireAdmin() is { } err) return err;
            var result = await _wagerAdminOrchestration.UpdatePlayerBalanceAsync(GetCurrentPlayerId(), request);
            return result.ToActionResult();
        }

        [HttpGet("players")]
        [ProducesResponseType(200, Type = typeof(List<AdminPlayerBalanceListItemModel>))]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetPlayersForBalanceAdmin()
        {
            if (RequireAdmin() is { } err) return err;
            var result = await _wagerAdminOrchestration.ListPlayersForBalanceAdminAsync();
            return result.ToActionResult();
        }

        [HttpGet("players/{playerId:int}/audit")]
        [ProducesResponseType(200, Type = typeof(List<WagerAuditEntryModel>))]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPlayerAudit(int playerId, [FromQuery] int? tournamentId = null)
        {
            if (RequireAdmin() is { } err) return err;
            var result = await _wagerAdminOrchestration.GetPlayerAuditAsync(playerId, tournamentId);
            return result.ToActionResult();
        }

        [HttpGet("players/{playerId:int}/tournament/{tournamentId:int}/summary")]
        [ProducesResponseType(200, Type = typeof(TournamentSummaryModel))]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPlayerTournamentSummary(int playerId, int tournamentId)
        {
            if (RequireAdmin() is { } err) return err;
            var result = await _wagerAdminOrchestration.GetPlayerTournamentSummaryAsync(playerId, tournamentId);
            return result.ToActionResult();
        }

        [HttpGet("settings")]
        [ProducesResponseType(200, Type = typeof(WagerSettingsModel))]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetWagerSettings()
        {
            if (RequireAdmin() is { } err) return err;
            var result = await _wagerAdminOrchestration.GetWagerSettingsAsync();
            return result.ToActionResult();
        }

        [HttpPut("settings")]
        [ProducesResponseType(200, Type = typeof(WagerSettingsModel))]
        [ProducesResponseType(403)]
        public async Task<IActionResult> UpdateWagerSettings([FromBody] WagerSettingsModel settings)
        {
            if (RequireAdmin() is { } err) return err;
            var result = await _wagerAdminOrchestration.UpdateWagerSettingsAsync(settings);
            return result.ToActionResult();
        }

        [HttpGet("audit")]
        [ProducesResponseType(200, Type = typeof(List<WagerAuditEntryModel>))]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetAllAudit([FromQuery] int? tournamentId = null)
        {
            if (RequireAdmin() is { } err) return err;
            var result = await _wagerAdminOrchestration.GetAllAuditAsync(tournamentId);
            return result.ToActionResult();
        }

        [HttpGet("pending-wagers")]
        [ProducesResponseType(200, Type = typeof(List<WagerModel>))]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetPendingWagers([FromQuery] int tournamentId)
        {
            if (RequireAdmin() is { } err) return err;
            if (tournamentId < 1)
                return BadRequest("tournamentId is required");
            var result = await _wagerAdminOrchestration.GetPendingWagersForTournamentAsync(tournamentId);
            return result.ToActionResult();
        }

        [HttpPost("wagers/{wagerId:int}/cancel")]
        [ProducesResponseType(200, Type = typeof(bool))]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AdminCancelWager(int wagerId)
        {
            if (RequireAdmin() is { } err) return err;
            var result = await _wagerAdminOrchestration.AdminCancelWagerAsync(GetCurrentPlayerId(), wagerId);
            return result.ToActionResult();
        }

        /// <summary>Bettable game payload for any state (completed/in progress); used by admin lines UI.</summary>
        [HttpGet("games/{gameResultId:int}/lines")]
        [ProducesResponseType(200, Type = typeof(BettableGameModel))]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetGameLinesForAdmin(int gameResultId)
        {
            if (RequireAdmin() is { } err) return err;
            var result = await _wagerOrchestration.GetGameDetailForAdminAsync(gameResultId);
            return result.ToActionResult();
        }

        [HttpPut("games/{gameResultId:int}/odds")]
        [ProducesResponseType(200, Type = typeof(bool))]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateGameOdds(int gameResultId, [FromBody] AdminUpdateGameOddsRequestModel body)
        {
            if (RequireAdmin() is { } err) return err;
            var result = await _wagerAdminOrchestration.UpdateGameOddsByGameResultIdAsync(gameResultId, body);
            return result.ToActionResult();
        }

        [HttpPost("game-result")]
        [ProducesResponseType(200, Type = typeof(SaveGameResultResponseModel))]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> SaveGameResult([FromBody] SaveGameResultRequestModel body)
        {
            if (RequireAdmin() is { } err) return err;
            var result = await _wagerAdminOrchestration.SaveGameResultAdminAsync(body);
            return result.ToActionResult();
        }

        [HttpGet("tournaments/{tournamentId:int}/wager-snapshot")]
        [ProducesResponseType(200, Type = typeof(WagerTournamentSnapshotModel))]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetWagerSnapshot(int tournamentId)
        {
            if (RequireAdmin() is { } err) return err;
            var result = await _wagerAdminOrchestration.GetWagerTournamentSnapshotAsync(tournamentId);
            return result.ToActionResult();
        }

        [HttpGet("tournaments/{tournamentId:int}/players/{playerId:int}/wagers")]
        [ProducesResponseType(200, Type = typeof(List<WagerModel>))]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetWagersForPlayerInTournament(int tournamentId, int playerId)
        {
            if (RequireAdmin() is { } err) return err;
            var result = await _wagerAdminOrchestration.GetWagersForPlayerTournamentAdminAsync(tournamentId, playerId);
            return result.ToActionResult();
        }

        [HttpGet("games/{gameResultId:int}/wagers")]
        [ProducesResponseType(200, Type = typeof(List<WagerModel>))]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetWagersForGame(int gameResultId, [FromQuery] int? tournamentId = null)
        {
            if (RequireAdmin() is { } err) return err;
            var result = await _wagerAdminOrchestration.GetWagersForGameAdminAsync(gameResultId, tournamentId);
            return result.ToActionResult();
        }
    }
}
