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

        //Generated Code
        [HttpGet("{tournamentId}")]
        public async Task<ActionResult<TournamentModel>> GetTournamentById(int tournamentId)
        {
            var tournament = await _tournamentsOrchestration.GetTournamentByIdAsync(tournamentId);
            if (tournament == null)
            {
                return NotFound();
            }
            return Ok(tournament);
        }

        [HttpGet("{tournamentId}/{tournamentId2}")]
        public async Task<ActionResult<IEnumerable<TournamentModel>>> GetTournamentByIds(int tournamentId, int tournamentId2)
        {
            var tournaments = await _tournamentsOrchestration.GetTournamentByIdsAsync(tournamentId, tournamentId2);
            return Ok(tournaments);
        }

        [HttpGet("{tournamentId}/{tournamentId2}/ifff")]
        public async Task<ActionResult<IEnumerable<TournamentModel>>> GetTournamentByIfff(int tournamentId, int tournamentId2)
        {
            var tournaments = await _tournamentsOrchestration.GetTournamentByIfffAsync(tournamentId, tournamentId2);
            return Ok(tournaments);
        }
        //End Generated Code
    }
}