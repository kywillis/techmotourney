using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;

namespace TecmoTourney.Orchestration.Interfaces
{
    public interface IGameResultOrchestration
    {
        Task<IEnumerable<GameResultModel>> ListResultsByTournamentAsync(int tourneyId);
        Task<IEnumerable<GameResultModel>> ListResultsByPlayerAsync(int playerId);
        Task<IEnumerable<GameResultModel>> SearchAsync(int player1Id, int player2Id);
        Task AddGameResultAsync(CreateGameResultRequestModel gameResult);
        Task UpdateGameResultAsync(int gameResultId, GameResultModel gameResult);
        Task DeleteGameResultAsync(int id);
    }
}
