using System.Numerics;
using TecmoTourney.Models;

namespace TecmoTourney.Orchestration.Interfaces
{
    public interface IPlayerOrchestration
    {
        Task<IEnumerable<CreatePlayerRequestModel>> GetPlayersAsync();
        Task<PlayerModel> GetPlayerAsync(int id);
        Task<PlayerModel> CreatePlayerAsync(CreatePlayerRequestModel player, IFormFile logo);
        Task<PlayerModel> UpdatePlayerAsync(int id, PlayerModel player);
        Task<PlayerModel> DeletePlayerAsync(int id);
    }
}
