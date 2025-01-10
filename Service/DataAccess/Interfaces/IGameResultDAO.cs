using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.DataAccess.Models;

namespace TecmoTourney.DataAccess.Interfaces
{
    public interface IGameResultDAO
    {
        Task<GameResultDAOModel> CreateGameResultAsync(GameResultDAOModel gameResult);
        Task DeleteGameResultAsync(int id);
        Task<GameResultDAOModel?> GetGameResultAsync(int gameResultId);
        Task<IEnumerable<GameResultDAOModel>> ListResultsByPlayerAsync(int playerId);
        Task<IEnumerable<GameResultDAOModel>> ListResultsByTournamentAsync(int tourneyId, bool includeDeledted = false);
        Task<IEnumerable<GameResultDAOModel>> ListResultsByBracketGameIDsAsync(IEnumerable<int> bracketGameIds);

        Task<IEnumerable<GameResultDAOModel>> SearchAsync(int tournamentId, int? player1Id, int? player2Id);
        Task UpdateGameResultAsync(int gameResultId, GameResultDAOModel gameResult);
    }
}
