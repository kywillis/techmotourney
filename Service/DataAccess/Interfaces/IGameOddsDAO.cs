using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.DataAccess.Models;

namespace TecmoTourney.DataAccess.Interfaces
{
    public interface IGameOddsDAO
    {
        Task<GameOddsDAOModel> CreatePointSpreadsAsync(GameOddsDAOModel gameOdds);
        Task<IEnumerable<GameOddsDAOModel>> GetByTournamentIdAsync(int tournamentId);
        Task<GameOddsDAOModel?> GetByGameResultIdAsync(int gameResultId);
        Task<int> UpdateByGameResultIdAsync(int gameResultId, decimal spread, int? favoredPlayerId, decimal? moneyLinePlayer1, decimal? moneyLinePlayer2, decimal? overUnder);
        Task DeleteByTournamentIdAsync(int tournamentId);
        Task<int> DeleteByGameResultIdAsync(int gameResultId);
        Task<int> DeleteByGameResultIdsAsync(IEnumerable<int> gameResultIds);
        Task<int> SoftDeleteByGameResultIdAsync(int gameResultId);
        Task<int> SoftDeleteByGameResultIdsAsync(IEnumerable<int> gameResultIds);
    }
}
