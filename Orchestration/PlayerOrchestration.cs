using TecmoTourney.Models;
using TecmoTourney.Orchestration.Interfaces;

namespace TecmoTourney.Orchestration
{
    public class PlayerOrchestration : IPlayerOrchestration
    {
        public Task<PlayerModel> CreatePlayerAsync(CreatePlayerRequestModel player, IFormFile logo)
        {
            throw new NotImplementedException();
        }

        public Task<PlayerModel> DeletePlayerAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<PlayerModel> GetPlayerAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<CreatePlayerRequestModel>> GetPlayersAsync()
        {
            throw new NotImplementedException();
        }

        public Task<PlayerModel> UpdatePlayerAsync(int id, PlayerModel player)
        {
            throw new NotImplementedException();
        }
    }
}
