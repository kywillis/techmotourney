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
        private readonly IWagerOrchestration _wagerOrchestration;

        public GameResultController(
            IGameResultOrchestration gameResultOrchestration,
            IWagerOrchestration wagerOrchestration)
        {
            _gameResultOrchestration = gameResultOrchestration;
            _wagerOrchestration = wagerOrchestration;
        }

        [HttpGet("tournament/{tournamentId}")]
        [ProducesResponseType(200, Type = typeof(List<GameResultModel>))]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ListResultsByTournament(int tournamentId, [FromQuery] bool includeDeleted = false)
        {
            var results = await _gameResultOrchestration.ListResultsByTournamentAsync(tournamentId, includeDeleted);
            return results.ToActionResult();
        }

        /// <summary>Public wagering snapshots for every game in the tournament that has odds (empty list if none).</summary>
        [HttpGet("tournament/{tournamentId:int}/wagering-snapshots")]
        [ProducesResponseType(200, Type = typeof(List<PublicWageringSnapshotModel>))]
        public async Task<IActionResult> GetWageringSnapshotsByTournament(int tournamentId)
        {
            var result = await _wagerOrchestration.GetPublicWageringSnapshotsByTournamentAsync(tournamentId);
            return result.ToActionResult();
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
        [ProducesResponseType(200, Type = typeof(SaveGameResultResponseModel))]
        public async Task<IActionResult> SaveGameResult([FromBody] SaveGameResultRequestModel gameResult)
        {
            var result = await _gameResultOrchestration.SaveGameResultAsync(gameResult);
            return result.ToActionResult();
        }

        [HttpPut("{gameResultId:int}")]
        [ProducesResponseType(200, Type = typeof(SaveGameResultResponseModel))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UpdateGameResult(int gameResultId, [FromBody] SaveGameResultRequestModel gameResult)
        {
            if (gameResult.GameResultId.HasValue && gameResult.GameResultId.Value != gameResultId)
            {
                return BadRequest("GameResultId in the request body must match the URL.");
            }

            gameResult.GameResultId = gameResultId;
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

        [HttpGet("{tournamentId}/pointSpreads")]
        [ProducesResponseType(200, Type = typeof(GameOddsModel[]))]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPointSpreads(int tournamentId)
        {
            var results = await _gameResultOrchestration.GetPointSpreadsAsync(tournamentId);
            return results.ToActionResult();
        }

        /// <summary>Public read-only lines, optional summary, and pending market depth for a game.</summary>
        [HttpGet("games/{gameResultId:int}/wagering-snapshot")]
        [ProducesResponseType(200, Type = typeof(PublicWageringSnapshotModel))]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetWageringSnapshot(int gameResultId)
        {
            var result = await _wagerOrchestration.GetPublicWageringSnapshotAsync(gameResultId);
            return result.ToActionResult();
        }
    }
}
