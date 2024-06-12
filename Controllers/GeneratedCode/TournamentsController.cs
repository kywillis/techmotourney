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
            var result = await _tournamentOrchestration.ListAll();
            return result.ToActionResult();
        }

        [HttpGet("{playerId}")]
        [ProducesResponseType(200, Type = typeof(ActionResult<Player>))]
        public async Task<IActionResult> ListResultsByPlayer(int playerId)
        {
            var result = await _tournamentOrchestration.ListResultsByPlayer(playerId);
            return result.ToActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> AddTournament([FromBody] CreateTournamentRequest tournament)
        {
            await _tournamentOrchestration.AddTournament(tournament);
            return Ok();
        }

        [HttpPut("{tournamentId}")]
        public async Task<IActionResult> UpdateTournament(int tournamentId, [FromBody] UpdateTournamentRequest tournament)
        {
            await _tournamentOrchestration.UpdateTournament(tournamentId, tournament);
            return Ok();
        }

        [HttpDelete("{tournamentId}")]
        public async Task<IActionResult> DeleteTournament(int tournamentId)
        {
            await _tournamentOrchestration.DeleteTournament(tournamentId);
            return Ok();
        }

        [HttpGet("{tournamentId}")]
        [ProducesResponseType(200, Type = typeof(ActionResult<Tournament>))]
        public async Task<IActionResult> GetTournamentById(int tournamentId)
        {
            var result = await _tournamentOrchestration.GetTournamentById(tournamentId);
            return result.ToActionResult();
        }

        [HttpGet("{tournamentId}/{tournamentId2}")]
        [ProducesResponseType(200, Type = typeof(ActionResult<Tournament[]>))]
        public async Task<IActionResult> GetTournamentByIds(int tournamentId, int tournamentId2)
        {
            var result = await _tournamentOrchestration.GetTournamentByIds(tournamentId, tournamentId2);
            return result.ToActionResult();
        }
    }
}