using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.Orchestration.Interfaces;

namespace TecmoTourney.Orchestration
{
    public class GameResultOrchestration : IGameResultOrchestration
    {
        public Task AddGameResultAsync(CreateGameResultRequestModel gameResult)
        {
            throw new NotImplementedException();
        }

        public Task DeleteGameResultAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<GameResultModel>> ListResultsByPlayerAsync(int playerId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<GameResultModel>> ListResultsByTournamentAsync(int tourneyId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<GameResultModel>> SearchAsync(int player1Id, int player2Id)
        {
            throw new NotImplementedException();
        }

        public Task UpdateGameResultAsync(int gameResultId, GameResultModel gameResult)
        {
            throw new NotImplementedException();
        }
    }
}
