using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SE.Zeigo.Admin.Orchestration;
using SE.Zeigo.Admin.Models;

namespace SE.Zeigo.Admin.Controllers
{
    [Route("tournaments")]
    [ApiController]
    [ProducesResponseType(400, Type = typeof(ErrorContent))]
    public class TournamentsController : ControllerBase
    {
        private readonly ITournamentsOrchestration _tournamentsOrchestration;

        public TournamentsController(ITournamentsOrchestration tournamentsOrchestration)
        {
            _tournamentsOrchestration = tournamentsOrchestration;
        }

        [HttpGet]
        [ProducesResponseType(200, Type = typeof(ActionResult<Tournament[]>))]
        public async Task<IActionResult> ListAll()
        {
            var result = await _tournamentsOrchestration.ListAll();
            return result.ToActionResult();
        }

        [HttpGet("player/{playerId}")]
        [ProducesResponseType(200, Type = typeof(ActionResult<Player>))]
        public async Task<IActionResult> ListResultsByPlayer(int playerId)
        {
            var result = await _tournamentsOrchestration.ListResultsByPlayer(playerId);
            return result.ToActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> AddTournament([FromBody] CreateTournamentRequest tournament)
        {
            await _tournamentsOrchestration.AddTournament(tournament);
            return Ok();
        }

        [HttpPut("{tournamentId}")]
        public async Task<IActionResult> UpdateTournament(int tournamentId, [FromBody] UpdateTournamentRequest tournament)
        {
            await _tournamentsOrchestration.UpdateTournament(tournamentId, tournament);
            return Ok();
        }

        [HttpDelete("{tournamentId}")]
        public async Task<IActionResult> DeleteTournament(int tournamentId)
        {
            await _tournamentsOrchestration.DeleteTournament(tournamentId);
            return Ok();
        }

        [HttpGet("{tournamentId}")]
        [ProducesResponseType(200, Type = typeof(ActionResult<Tournament>))]
        public async Task<IActionResult> GetTournamentById(int tournamentId)
        {
            var result = await _tournamentsOrchestration.GetTournamentById(tournamentId);
            return result.ToActionResult();
        }

        [HttpGet("{tournamentId}/{tournamentId2}")]
        [ProducesResponseType(200, Type = typeof(ActionResult<Tournament[]>))]
        public async Task<IActionResult> GetTournamentByIds(int tournamentId, int tournamentId2)
        {
            var result = await _tournamentsOrchestration.GetTournamentByIds(tournamentId, tournamentId2);
            return result.ToActionResult();
        }
    }
}