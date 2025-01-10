using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.DataAccess.Models;

namespace TecmoTourney.DataAccess.Interfaces
{
    public interface IPlayerDAO
    {
        Task<IEnumerable<PlayerDAOModel>> ListPlayersAsync(int? tourneyId = null);
        Task<PlayerDAOModel?> GetPlayerAsync(int id);
        Task<PlayerDAOModel> AddPlayerAsync(PlayerDAOModel player);
        Task<PlayerDAOModel> UpdatePlayerAsync(int id, PlayerDAOModel player);
        Task<bool> DeletePlayerAsync(int id);
    }
}
