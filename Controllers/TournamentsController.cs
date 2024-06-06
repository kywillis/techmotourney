using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.Orchestration.Interfaces;

namespace TecmoTourney.Controllers
{
    [ApiController]
    [Route("api/tournaments")]
    public class TournamentsController : ControllerBase
    {
        private readonly ITournamentsOrchestration _tournamentsOrchestration;

        public TournamentsController(ITournamentsOrchestration tournamentsOrchestration)
        {
            _tournamentsOrchestration = tournamentsOrchestration;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TournamentModel>>> ListAll()
        {
            var tournaments = await _tournamentsOrchestration.ListAllAsync();
            return Ok(tournaments);
        }

        [HttpGet("player/{playerId}")]
        public async Task<ActionResult<PlayerModel>> ListResultsByPlayer(int playerId)
        {
            var player = await _tournamentsOrchestration.ListResultsByPlayerAsync(playerId);
            if (player == null)
            {
                return NotFound();
            }
            return Ok(player);
        }

        [HttpPost]
        public async Task<IActionResult> AddTournament([FromBody] CreateTournamentRequestModel tournament)
        {
            await _tournamentsOrchestration.AddTournamentAsync(tournament);
            return Ok();
        }

        [HttpPut("{tournamentId}")]
        public async Task<IActionResult> UpdateTournament(int tournamentId, [FromBody] UpdateTournamentRequestModel tournament)
        {
            await _tournamentsOrchestration.UpdateTournamentAsync(tournamentId, tournament);
            return Ok();
        }

        [HttpDelete("{tournamentId}")]
        public async Task<IActionResult> DeleteTournament(int tournamentId)
        {
            await _tournamentsOrchestration.DeleteTournamentAsync(tournamentId);
            return Ok();
        }
    }
}
