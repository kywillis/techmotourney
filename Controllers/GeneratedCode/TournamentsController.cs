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
        private readonly ITournamentsOrchestration _tournamentsOrchestration;

        public TournamentsController(ITournamentsOrchestration tournamentsOrchestration)
        {
            _tournamentsOrchestration = tournamentsOrchestration;
        }

        [HttpGet]
        [ProducesResponseType(200, Type = typeof(ActionResult<Tournament[]>))]
        public async Task<IActionResult> ListAll()
        {
            var result = await _tournamentsOrchestration.ListAllAsync();
            return result.ToActionResult();
        }

        [HttpGet("{playerId}")]
        [ProducesResponseType(200, Type = typeof(ActionResult<Player>))]
        public async Task<IActionResult> ListResultsByPlayer(int playerId)
        {
            var result = await _tournamentsOrchestration.ListResultsByPlayerAsync(playerId);
            return result.ToActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> AddTournament([FromBody] CreateTournamentRequest tournament)
        {
            await _tournamentsOrchestration.AddTournamentAsync(tournament);
            return Ok();
        }

        [HttpPut("{tournamentId}")]
        public async Task<IActionResult> UpdateTournament(int tournamentId, [FromBody] UpdateTournamentRequest tournament)
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

        [HttpGet("{tournamentId}")]
        [ProducesResponseType(200, Type = typeof(ActionResult<Tournament>))]
        public async Task<IActionResult> GetTournamentById(int tournamentId)
        {
            var result = await _tournamentsOrchestration.GetTournamentByIdAsync(tournamentId);
            return result.ToActionResult();
        }

        [HttpGet("{tournamentId}/{tournamentId2}")]
        [ProducesResponseType(200, Type = typeof(ActionResult<Tournament[]>))]
        public async Task<IActionResult> GetTournamentByIds(int tournamentId, int tournamentId2)
        {
            var result = await _tournamentsOrchestration.GetTournamentByIdsAsync(tournamentId, tournamentId2);
            return result.ToActionResult();
        }

        [HttpGet("{tournamentId}/{tournamentId2}")]
        [ProducesResponseType(200, Type = typeof(ActionResult<Tournament[]>))]
        public async Task<IActionResult> GetTournamentByIds2(int tournamentId, int tournamentId2)
        {
            var result = await _tournamentsOrchestration.GetTournamentByIds2Async(tournamentId, tournamentId2);
            return result.ToActionResult();
        }
    }
}