using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.ResultPattern;

namespace TecmoTourney.Orchestration.Interfaces
{
    public interface IGameStationOrchestration
    {
        Task<Operation<GameStationGamesResponseModel, ApiError>> GetGamesForActiveTournamentAsync();

        Task<Operation<GameResultModel, ApiError>> UpdateGameAsync(int gameResultId, GameStationUpdateRequestModel request);
    }
}
