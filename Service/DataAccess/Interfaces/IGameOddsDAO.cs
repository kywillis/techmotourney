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
        Task<int> UpdateByGameResultIdAsync(int gameResultId, int spread, int? favoredPlayerId, int? moneyLinePlayer1, int? moneyLinePlayer2, decimal? overUnder);
        Task DeleteByTournamentIdAsync(int tournamentId);
    }
}
