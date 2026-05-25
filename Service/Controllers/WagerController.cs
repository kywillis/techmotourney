using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecmoTourney;
using TecmoTourney.Middleware;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.Orchestration.Interfaces;
using TecmoTourney.ResultPattern;

namespace TecmoTourney.Controllers
{
    [ApiController]
    [Route("api/wager")]
    //[Authorize(AuthenticationSchemes = "Google")]
    public class WagerController : ControllerBase
    {
        private readonly IWagerOrchestration _wagerOrchestration;
        private readonly ITournamentsOrchestration _tournamentsOrchestration;

        public WagerController(IWagerOrchestration wagerOrchestration, ITournamentsOrchestration tournamentsOrchestration)
        {
            _wagerOrchestration = wagerOrchestration;
            _tournamentsOrchestration = tournamentsOrchestration;
        }

        private int GetCurrentPlayerId()
        {
            var id = HttpContext.GetWagerPlayerId();
            if (id == null)
                throw new InvalidOperationException("WagerPlayerId not set. Ensure request is authenticated and WagerPlayerResolutionMiddleware ran.");
            return id.Value;
        }

        [HttpGet("balance")]
        [ProducesResponseType(200, Type = typeof(decimal))]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetBalance()
        {
            var result = await _wagerOrchestration.GetBalanceAsync(GetCurrentPlayerId());
            return result.ToActionResult();
        }

        [HttpGet("wagers")]
        [ProducesResponseType(200, Type = typeof(List<WagerModel>))]
        public async Task<IActionResult> GetMyWagers([FromQuery] WagerStatus? status = null, [FromQuery] int? tournamentId = null)
        {
            var result = await _wagerOrchestration.GetMyWagersAsync(GetCurrentPlayerId(), status, tournamentId);
            return result.ToActionResult();
        }

        [HttpGet("audit")]
        [ProducesResponseType(200, Type = typeof(List<WagerAuditEntryModel>))]
        public async Task<IActionResult> GetMyAudit([FromQuery] int? tournamentId = null)
        {
            var result = await _wagerOrchestration.GetMyAuditAsync(GetCurrentPlayerId(), tournamentId);
            return result.ToActionResult();
        }

        [HttpGet("audit/final-balance")]
        [ProducesResponseType(200, Type = typeof(decimal?))]
        public async Task<IActionResult> GetFinalBalanceForTournament([FromQuery] int tournamentId)
        {
            var result = await _wagerOrchestration.GetFinalBalanceForTournamentAsync(GetCurrentPlayerId(), tournamentId);
            return result.ToActionResult();
        }

        [HttpGet("games")]
        [ProducesResponseType(200, Type = typeof(List<BettableGameModel>))]
        public async Task<IActionResult> GetGamesAvailableToBet()
        {
            var result = await _wagerOrchestration.GetGamesAvailableToBetAsync();
            return result.ToActionResult();
        }

        /// <summary>GET <c>api/wager/wager-games-board</c> — single segment so SPA/static routing never collides with <c>games/{id}</c>.</summary>
        [HttpGet("wager-games-board")]
        [ProducesResponseType(200, Type = typeof(WagerGamesBoardModel))]
        public async Task<IActionResult> GetGamesBoard()
        {
            var result = await _wagerOrchestration.GetGamesBoardAsync();
            return result.ToActionResult();
        }

        [HttpGet("games/{gameResultId:int}")]
        [ProducesResponseType(200, Type = typeof(BettableGameModel))]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetGameDetailForWager(int gameResultId)
        {
            var result = await _wagerOrchestration.GetGameDetailForWagerAsync(gameResultId);
            return result.ToActionResult();
        }

        [HttpGet("tournament/active")]
        [ProducesResponseType(200, Type = typeof(TournamentModel))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetActiveTournament()
        {
            var result = await _tournamentsOrchestration.GetActive();
            return result.ToActionResult();
        }

        [HttpGet("tournament")]
        [ProducesResponseType(200, Type = typeof(List<TournamentModel>))]
        public async Task<IActionResult> GetTournaments()
        {
            var result = await _tournamentsOrchestration.ListAllAsync();
            return result.ToActionResult();
        }

        [HttpGet("tournament/{tournamentId}/summary")]
        [ProducesResponseType(200, Type = typeof(TournamentSummaryModel))]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetTournamentSummary(int tournamentId)
        {
            var result = await _wagerOrchestration.GetTournamentSummaryForUserAsync(GetCurrentPlayerId(), tournamentId);
            return result.ToActionResult();
        }

        [HttpPost("wagers")]
        [ProducesResponseType(200, Type = typeof(WagerModel))]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> PlaceWager([FromBody] PlaceWagerRequestModel request)
        {
            var result = await _wagerOrchestration.PlaceWagerAsync(GetCurrentPlayerId(), request);
            return result.ToActionResult();
        }
    }
}
