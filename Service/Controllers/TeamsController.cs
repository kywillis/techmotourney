using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.Models;
using TecmoTourney.Orchestration.Interfaces;
using TecmoTourney.ResultPattern;

namespace TecmoTourney.Controllers
{
    [ApiController]
    [Route("api/gameTeams")]
    public class GameTeamsController : ControllerBase
    {
        private readonly IGameTeamOrchestration _gameTeamOrchestration;

        public GameTeamsController(IGameTeamOrchestration gameTeamOrchestration)
        {
            _gameTeamOrchestration = gameTeamOrchestration;
        }

        /// <summary>
        /// Get all Game Teams
        /// </summary>
        /// <returns>List of game teams</returns>
        [HttpGet()]
        [ProducesResponseType(200, Type = typeof(List<GameTeamModel>))]
        public async Task<IActionResult> GetGameTeams()
        {
            var result = await _gameTeamOrchestration.GetAll();
            return result.ToActionResult();
        }

        [HttpGet("/test")]
        [ProducesResponseType(200, Type = typeof(List<GameTeamModel>))]
        public async Task<TournamentBracketModel> Test()
        {
            await Task.Delay(0);
            var bracket = new TournamentBracketModel();
            bracket.PopulateBracket(4);
            return bracket;
        }
    }
}
