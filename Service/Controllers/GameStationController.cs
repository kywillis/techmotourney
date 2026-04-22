using Microsoft.AspNetCore.Mvc;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.Orchestration.Interfaces;
using TecmoTourney.ResultPattern;

namespace TecmoTourney.Controllers;

[ApiController]
[Route("api/game-station")]
public class GameStationController : ControllerBase
{
    private readonly IGameStationOrchestration _gameStationOrchestration;

    public GameStationController(IGameStationOrchestration gameStationOrchestration)
    {
        _gameStationOrchestration = gameStationOrchestration;
    }

    /// <summary>Waiting and in-progress games for the single active tournament.</summary>
    [HttpGet("games")]
    [ProducesResponseType(200, Type = typeof(GameStationGamesResponseModel))]
    public async Task<IActionResult> GetGames()
    {
        var result = await _gameStationOrchestration.GetGamesForActiveTournamentAsync();
        return result.ToActionResult();
    }

    /// <summary>Set teams and start a waiting game (in progress + started timestamp) or update teams on an in-progress game.</summary>
    [HttpPut("games/{gameResultId:int}")]
    [ProducesResponseType(200, Type = typeof(GameResultModel))]
    public async Task<IActionResult> UpdateGame(int gameResultId, [FromBody] GameStationUpdateRequestModel body)
    {
        var result = await _gameStationOrchestration.UpdateGameAsync(gameResultId, body);
        return result.ToActionResult();
    }
}
