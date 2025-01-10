using Microsoft.AspNetCore.Mvc;
using TecmoTourney.Models;
using TecmoTourney.Orchestration.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.Models.Requests;
using TecmoTourney.ResultPattern;

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

        [HttpGet("tournament/{tournamentId}")]
        [ProducesResponseType(200, Type = typeof(List<GameResultModel>))]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ListResultsByTournament(int tournamentId, [FromQuery] bool includeDeleted = false)
        {
            var results = await _gameResultOrchestration.ListResultsByTournamentAsync(tournamentId, includeDeleted);
            return results.ToActionResult();
        }

        [HttpGet("player/{playerId}")]
        [ProducesResponseType(200, Type = typeof(List<GameResultModel>))]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ListResultsByPlayer(int playerId)
        {
            var results = await _gameResultOrchestration.ListResultsByPlayerAsync(playerId);
            return results.ToActionResult();
        }

        [HttpGet("search")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<GameResultModel>))]
        public async Task<IActionResult> Search([FromQuery] int tournamentId, [FromQuery] int? player1Id, [FromQuery] int? player2Id)
        {
            var results = await _gameResultOrchestration.SearchAsync(tournamentId, player1Id, player2Id);
            return results.ToActionResult();
        }

        [HttpPost]
        [ProducesResponseType(200, Type = typeof(GameResultModel))]
        public async Task<IActionResult> SaveGameResult([FromBody] SaveGameResultRequestModel gameResult)
        {
            var result = await _gameResultOrchestration.SaveGameResultAsync(gameResult);
            return result.ToActionResult();
        }

        //[HttpPut("{gameResultId}")]
        //[ProducesResponseType(200, Type = typeof(List<GameResultModel>))]
        //[ProducesResponseType(401)]
        //public async Task<IActionResult> UpdateGameResult(int gameResultId, [FromBody] GameResultModel gameResult)
        //{
        //    var result = await _gameResultOrchestration.UpdateGameResultAsync(gameResultId, gameResult);
        //    return result.ToActionResult();
        //}

        [HttpDelete("{gameResultId}")]
        [ProducesResponseType(200, Type = typeof(bool))]
        public async Task<IActionResult> DeleteGameResult(int gameResultId)
        {
            var result = await _gameResultOrchestration.DeleteGameResultAsync(gameResultId);
            return result.ToActionResult();
        }
    }
}
