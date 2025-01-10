using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.DataAccess.Models;

namespace TecmoTourney.DataAccess.Interfaces
{
    public interface IPlayerTournamentDAO
    {
        Task<int> CreatePlayerTournamentAsync(PlayerTournamentDAOModel playerTournament);
        Task<IEnumerable<PlayerTournamentDAOModel>> GetByTournamentIdAsync(int tournamentId);
        Task<IEnumerable<PlayerTournamentDAOModel>> GetByPlayerIdAsync(int playerId);
        Task DeleteByPlayerAndTournamentIdAsync(int playerId, int tournamentId);
    }
}
