using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.DataAccess.Models;

namespace TecmoTourney.DataAccess.Interfaces
{
    public interface IPointSpreadDAO
    {
        Task<PointSpreadDAOModel> CreatePointSpreadsAsync(PointSpreadDAOModel pointSpread);
        Task<IEnumerable<PointSpreadDAOModel>> GetByTournamentIdAsync(int tournamentId);
        Task DeleteByTournamentIdAsync(int tournamentId);
    }
}
