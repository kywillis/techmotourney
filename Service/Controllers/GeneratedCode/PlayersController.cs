using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using TecmoTourney.Models;
using TecmoTourney.Orchestration.Interfaces;

namespace TecmoTourney.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayersController : ControllerBase
    {
        private readonly IPlayerOrchestration _playerOrchestration;

        public PlayersController(IPlayerOrchestration playerOrchestration)
        {
            _playerOrchestration = playerOrchestration;
        }

        //Generated Code
        [HttpGet]
        [ProducesResponseType(200, Type = typeof(ActionResult<IEnumerable<PlayerModel>>))]
        public async Task<IActionResult> ListPlayers([FromQuery] int tourneyId)
        {
            var players = await _playerOrchestration.ListPlayers(tourneyId);
            return players.ToActionResult();
        }

        [HttpPost("addPlayerToTournament/{playerId}")]
        public async Task<IActionResult> AddPlayerToTournament(int playerId, [FromQuery] int tourneyId)
        {
            await _playerOrchestration.AddPlayerToTournament(playerId, tourneyId);
            return Ok();
        }

        [HttpDelete("removePlayerFromTournament/{playerId}")]
        public async Task<IActionResult> RemovePlayerFromTournament(int playerId, [FromQuery] int tourneyId)
        {
            await _playerOrchestration.RemovePlayerFromTournament(playerId, tourneyId);
            return Ok();
        }
        //End Generated Code
    }
}