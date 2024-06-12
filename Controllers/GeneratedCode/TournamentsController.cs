using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SE.Zeigo.Admin.Orchestration;
using SE.Zeigo.Admin.Models;

namespace SE.Zeigo.Admin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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

        [HttpGet("{playerId}")]
        [ProducesResponseType(200, Type = typeof(ActionResult<Player>))]
        public async Task<IActionResult> ListResultsByPlayer(int playerId)
        {
            var result = await _tournamentsOrchestration.ListResultsByPlayer(playerId);
            return result.ToActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> AddTournament([FromBody] CreateTournamentRequest tournament)
        {
            var result = await _tournamentsOrchestration.AddTournament(tournament);
            return result.ToActionResult();
        }

        [HttpPut("{tournamentId}")]
        public async Task<IActionResult> UpdateTournament(int tournamentId, [FromBody] UpdateTournamentRequest tournament)
        {
            var result = await _tournamentsOrchestration.UpdateTournament(tournamentId, tournament);
            return result.ToActionResult();
        }

        [HttpDelete("{tournamentId}")]
        public async Task<IActionResult> DeleteTournament(int tournamentId)
        {
            var result = await _tournamentsOrchestration.DeleteTournament(tournamentId);
            return result.ToActionResult();
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

        [HttpGet("{tournamentId}/{tournamentId2}")]
        [ProducesResponseType(200, Type = typeof(ActionResult<Tournament[]>))]
        public async Task<IActionResult> GetTournamentByIfff(int tournamentId, int tournamentId2)
        {
            var result = await _tournamentsOrchestration.GetTournamentByIfff(tournamentId, tournamentId2);
            return result.ToActionResult();
        }
    }
}