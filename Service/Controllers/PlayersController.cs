using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.Models;
using TecmoTourney.Orchestration.Interfaces;
using TecmoTourney.ResultPattern;

namespace TecmoTourney.Controllers
{
    [ApiController]
    [Route("api/players")]
    public class PlayersController : ControllerBase
    {
        private readonly IPlayerOrchestration _playerOrchestration;

        public PlayersController(IPlayerOrchestration playerOrchestration)
        {
            _playerOrchestration = playerOrchestration;
        }

        /// <summary>
        /// Get players by tournament ID
        /// </summary>
        /// <param name="tournamentId">Tournament ID</param>
        /// <returns>List of players</returns>
        [HttpGet("tournament/{tournamentId}")]
        [ProducesResponseType(200, Type = typeof(List<PlayerModel>))]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPlayers(int tournamentId)
        {
            var result = await _playerOrchestration.GetPlayersAsync(tournamentId);
            return result.ToActionResult();
        }

        /// <summary>
        /// Get all players
        /// </summary>
        /// <param name="tournamentId">Tournament ID</param>
        /// <returns>List of players</returns>
        [HttpGet()]
        [ProducesResponseType(200, Type = typeof(List<PlayerModel>))]
        public async Task<IActionResult> GetAllPlayers()
        {
            var result = await _playerOrchestration.GetAllPlayersAsync();
            return result.ToActionResult();
        }

        /// <summary>
        /// Get player by ID
        /// </summary>
        /// <param name="id">Player ID</param>
        /// <returns>Player details</returns>
        [HttpGet("{playerId}")]
        [ProducesResponseType(200, Type = typeof(PlayerModel))]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPlayer(int playerId)
        {
            var result = await _playerOrchestration.GetPlayerAsync(playerId);
            return result.ToActionResult();
        }

        /// <summary>
        /// Add a new player
        /// </summary>
        /// <param name="player">Player data</param>
        /// <param name="logo">Player logo</param>
        /// <returns>Created player details</returns>
        [HttpPost]
        [ProducesResponseType(201, Type = typeof(PlayerModel))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AddPlayer(CreatePlayerRequestModel player)
        {
            var result = await _playerOrchestration.CreatePlayerAsync(player);
            return result.ToActionResult();
        }

        /// <summary>
        /// Update player details
        /// </summary>
        /// <param name="playerId">Player ID</param>
        /// <param name="player">Updated player data</param>
        /// <returns>No content</returns>
        [HttpPut("{playerId}")]
        [ProducesResponseType(204, Type = typeof(PlayerModel))]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdatePlayer(int playerId, [FromBody] PlayerModel player)
        {
            if (playerId != player.PlayerId)
            {
                return BadRequest(new ApiError("Invalid player ID", System.Net.HttpStatusCode.BadRequest));
            }

            var result = await _playerOrchestration.UpdatePlayerAsync(playerId, player);
            return result.ToActionResult();
        }

        /// <summary>
        /// Delete player by ID
        /// </summary>
        /// <param name="id">Player ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{playerId}")]
        [ProducesResponseType(204, Type = typeof(PlayerModel))]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeletePlayer(int playerId)
        {
            var result = await _playerOrchestration.DeletePlayerAsync(playerId);
            return result.ToActionResult();
        }

        /// <summary>
        /// Add player to tournament
        /// </summary>
        /// <param name="id">Player ID</param>
        /// <param name="tournamentId">Tournament ID</param>
        /// <returns>Ok result</returns>
        [HttpPost("{playerId}/addToTournament/{tournamentId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AddPlayerToTournament(int playerId, int tournamentId)
        {
            var result = await _playerOrchestration.AddPlayerToTournament(playerId, tournamentId);
            return result.ToActionResult();
        }

        /// <summary>
        /// Remove player from tournament
        /// </summary>
        /// <param name="playerId">Player ID</param>
        /// <param name="tournamentId">Tournament ID</param>
        /// <returns>Ok result</returns>
        [HttpDelete("{playerId}/removeFromTournament/{tournamentId}")]
        [ProducesResponseType(200, Type = typeof(bool))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> RemovePlayerFromTournament(int playerId, int tournamentId)
        {
            var result = await _playerOrchestration.RemovePlayerFromTournament(playerId, tournamentId);
            return result.ToActionResult();
        }
    }
}
