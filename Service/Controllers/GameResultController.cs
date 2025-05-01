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
        public async Task<IActionResult> Search([FromQuery] int? tournamentId, [FromQuery] int? player1Id, [FromQuery] int? player2Id, [FromQuery] BracketLocation? bracketLocation)
        {
            var results = await _gameResultOrchestration.SearchAsync(tournamentId, player1Id, player2Id, bracketLocation);
            return results.ToActionResult();
        }

        [HttpPost]
        [ProducesResponseType(200, Type = typeof(GameResultModel))]
        public async Task<IActionResult> SaveGameResult([FromBody] SaveGameResultRequestModel gameResult)
        {
            var result = await _gameResultOrchestration.SaveGameResultAsync(gameResult);
            return result.ToActionResult();
        }

        [HttpGet("gameUpdates/{tournamentId}")]
        [ProducesResponseType(200, Type = typeof(List<TournamentBracketUpdateModel>))]
        [ProducesResponseType(401)]
        public async Task<IActionResult> UpdateGameResult(int tournamentId)
        {
            var result = await _gameResultOrchestration.GetGameUpdates(tournamentId);
            return result.ToActionResult();
        }

        [HttpPut("gameUpdates/{tournamentBracketUpdateId}")]
        [ProducesResponseType(200, Type = typeof(List<TournamentBracketUpdateModel>))]
        [ProducesResponseType(401)]
        public async Task<IActionResult> AcknowledgeBracketUpdate(int tournamentBracketUpdateId)
        {
            var result = await _gameResultOrchestration.AcknowledgeBracketUpdate(tournamentBracketUpdateId);
            return result.ToActionResult();
        }

        [HttpDelete("{gameResultId}")]
        [ProducesResponseType(200, Type = typeof(bool))]
        public async Task<IActionResult> DeleteGameResult(int gameResultId)
        {
            var result = await _gameResultOrchestration.DeleteGameResultAsync(gameResultId);
            return result.ToActionResult();
        }

        [HttpPost("{tournamentId}/pointSpreads")]
        [ProducesResponseType(200, Type = typeof(PointSpreadModel[]))]
        [ProducesResponseType(404)]
        public async Task<IActionResult> CreatePointSpreads(int tournamentId, IEnumerable<PointSpreadRequestModel> pointSpreads)
        {
            var results = await _gameResultOrchestration.CreatePointSpreadsAsync(tournamentId, pointSpreads);
            return results.ToActionResult();
        }

        [HttpGet("{tournamentId}/pointSpreads")]
        [ProducesResponseType(200, Type = typeof(PointSpreadModel[]))]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPointSpreads(int tournamentId)
        {
            var results = await _gameResultOrchestration.GetPointSpreadsAsync(tournamentId);
            return results.ToActionResult();
        }
    }
}
