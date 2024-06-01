using Microsoft.AspNetCore.Mvc;
using TecmoTourney.Models;
using TecmoTourney.Orchestration.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.Models.Requests;

namespace TecmoTourney.Controllers
{
    [ApiController]
    [Route("api/results")]
    public class GameResultController : ControllerBase
    {
        private readonly IGameResultOrchestration _gameResultOrchestration;

        public GameResultController(IGameResultOrchestration gameResultOrchestration)
        {
            _gameResultOrchestration = gameResultOrchestration;
        }

        [HttpGet("tournament/{tourneyId}")]
        public async Task<ActionResult<IEnumerable<GameResultModel>>> ListResultsByTournament(int tourneyId)
        {
            var results = await _gameResultOrchestration.ListResultsByTournamentAsync(tourneyId);
            return Ok(results);
        }

        [HttpGet("player/{playerId}")]
        public async Task<ActionResult<IEnumerable<GameResultModel>>> ListResultsByPlayer(int playerId)
        {
            var results = await _gameResultOrchestration.ListResultsByPlayerAsync(playerId);
            return Ok(results);
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<GameResultModel>>> Search([FromQuery] int player1Id, [FromQuery] int player2Id)
        {
            var results = await _gameResultOrchestration.SearchAsync(player1Id, player2Id);
            return Ok(results);
        }

        [HttpPost]
        public async Task<IActionResult> AddGameResult([FromBody] CreateGameResultRequestModel gameResult)
        {
            await _gameResultOrchestration.AddGameResultAsync(gameResult);
            return Ok();
        }

        [HttpPut("{gameResultId}")]
        public async Task<IActionResult> UpdateGameResult(int gameResultId, [FromBody] GameResultModel gameResult)
        {
            await _gameResultOrchestration.UpdateGameResultAsync(gameResultId, gameResult);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGameResult(int id)
        {
            await _gameResultOrchestration.DeleteGameResultAsync(id);
            return Ok();
        }
    }
}
