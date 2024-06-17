using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using TecmoTourney.Models;
using TecmoTourney.Orchestration.Interfaces;

namespace TecmoTourney.Controllers
{
    [ApiController]
    [Route("api/player")]
    public class PlayersController : ControllerBase
    {
        private readonly IPlayerOrchestration _playerOrchestration;

        public PlayersController(IPlayerOrchestration playerOrchestration)
        {
            _playerOrchestration = playerOrchestration;
        }

        [HttpGet("tournaments/{tournamentId}")]
        public async Task<ActionResult<IEnumerable<CreatePlayerRequestModel>>> GetPlayers(int tournamentId)
        {
            var players = await _playerOrchestration.GetPlayersAsync(tournamentId);
            return Ok(players);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CreatePlayerRequestModel>> GetPlayer(int id)
        {
            var player = await _playerOrchestration.GetPlayerAsync(id);
            if (player == null)
            {
                return NotFound();
            }
            return Ok(player);
        }

        [HttpPost]
        public async Task<ActionResult<PlayerModel>> AddPlayer([FromForm] CreatePlayerRequestModel player, [FromForm] IFormFile logo)
        {
            var newPlayer = await _playerOrchestration.CreatePlayerAsync(player, logo);
            return CreatedAtAction(nameof(GetPlayer), new { id = newPlayer.PlayerId }, newPlayer);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePlayer(int id, [FromBody] PlayerModel player)
        {
            if (id != player.PlayerId)
            {
                return BadRequest();
            }

            var updatedPlayer = await _playerOrchestration.UpdatePlayerAsync(id, player);
            if (updatedPlayer == null)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlayer(int id)
        {
            var player = await _playerOrchestration.DeletePlayerAsync(id);
            if (player == null)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpPost("addPlayerToTournament/{id}")]
        public async Task<IActionResult> AddPlayerToTournament(int id, [FromQuery] int tournamentId)
        {
            await _playerOrchestration.AddPlayerToTournament(id, tournamentId);
            return Ok();
        }

        [HttpDelete("removePlayerFromTournament/{id}")]
        public async Task<IActionResult> RemovePlayerFromTournament(int id, [FromQuery] int tournamentId)
        {
            await _playerOrchestration.RemovePlayerFromTournament(id, tournamentId);
            return Ok();
        }
    }
}
