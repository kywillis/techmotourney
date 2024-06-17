using System.Numerics;
using TecmoTourney.Models;

namespace TecmoTourney.Orchestration.Interfaces
{
    public interface IPlayerOrchestration
    {
        Task<IEnumerable<PlayerModel>> GetPlayersAsync(int tourneyId);

        Task<PlayerModel> GetPlayerAsync(int playerId);
        Task<PlayerModel> CreatePlayerAsync(CreatePlayerRequestModel player, IFormFile logo);
        Task<PlayerModel> UpdatePlayerAsync(int playerId, PlayerModel player);
        Task<PlayerModel> DeletePlayerAsync(int playerId);
        Task AddPlayerToTournament(int playerId, int tourneyId);
        Task RemovePlayerFromTournament(int playerId, int tourneyId);
    }
}
