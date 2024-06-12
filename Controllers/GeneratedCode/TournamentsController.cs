using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SE.Zeigo.Admin.Orchestration;
using SE.Zeigo.Admin.Models;

namespace SE.Zeigo.Admin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ProducesResponseType(400, Type = typeof(ErrorContent))]
    public class TournamentsController : ControllerBase
    {
        private readonly ITournamentOrchestration _tournamentOrchestration;

        public TournamentsController(ITournamentOrchestration tournamentOrchestration)
        {
            _tournamentOrchestration = tournamentOrchestration;
        }

        [HttpGet]
        [ProducesResponseType(200, Type = typeof(ActionResult<Tournament[]>))]
        public async Task<IActionResult> ListAll()
        {
            var tournaments = await _tournamentOrchestration.ListAllAsync();
            return tournaments.ToActionResult();
        }

        [HttpGet("{playerId}")]
        [ProducesResponseType(200, Type = typeof(ActionResult<Player>))]
        public async Task<IActionResult> ListResultsByPlayer(int playerId)
        {
            var player = await _tournamentOrchestration.ListResultsByPlayerAsync(playerId);
            return player.ToActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> AddTournament([FromBody] CreateTournamentRequest tournament)
        {
            await _tournamentOrchestration.AddTournamentAsync(tournament);
            return Ok();
        }

        [HttpPut("{tournamentId}")]
        public async Task<IActionResult> UpdateTournament(int tournamentId, [FromBody] UpdateTournamentRequest tournament)
        {
            await _tournamentOrchestration.UpdateTournamentAsync(tournamentId, tournament);
            return Ok();
        }

        [HttpDelete("{tournamentId}")]
        public async Task<IActionResult> DeleteTournament(int tournamentId)
        {
            await _tournamentOrchestration.DeleteTournamentAsync(tournamentId);
            return Ok();
        }

        [HttpGet("{tournamentId}")]
        [ProducesResponseType(200, Type = typeof(ActionResult<Tournament>))]
        public async Task<IActionResult> GetTournamentById(int tournamentId)
        {
            var tournament = await _tournamentOrchestration.GetTournamentByIdAsync(tournamentId);
            return tournament.ToActionResult();
        }

        [HttpGet("{tournamentId}/{tournamentId2}")]
        [ProducesResponseType(200, Type = typeof(ActionResult<Tournament[]>))]
        public async Task<IActionResult> GetTournamentByIds(int tournamentId, int tournamentId2)
        {
            var tournaments = await _tournamentOrchestration.GetTournamentByIdsAsync(tournamentId, tournamentId2);
            return tournaments.ToActionResult();
        }

        [HttpGet("{tournamentId}/{tournamentId2}")]
        [ProducesResponseType(200, Type = typeof(ActionResult<Tournament[]>))]
        public async Task<IActionResult> GetTournamentByIfff(int tournamentId, int tournamentId2)
        {
            var tournaments = await _tournamentOrchestration.GetTournamentByIfffAsync(tournamentId, tournamentId2);
            return tournaments.ToActionResult();
        }
    }
}