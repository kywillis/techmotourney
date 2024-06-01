using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.DataAccess.Models;

namespace TecmoTourney.DataAccess.Interfaces
{
    public interface IGameResultDAO
    {
        Task<IEnumerable<GameResultDAOModel>> ListResultsByTournamentAsync(int tourneyId);
        Task<IEnumerable<GameResultDAOModel>> ListResultsByPlayerAsync(int playerId);
        Task<IEnumerable<GameResultDAOModel>> SearchAsync(int player1Id, int player2Id);
        Task AddGameResultAsync(GameResultDAOModel gameResult);
        Task UpdateGameResultAsync(int gameResultId, GameResultDAOModel gameResult);
        Task DeleteGameResultAsync(int id);
    }
}
