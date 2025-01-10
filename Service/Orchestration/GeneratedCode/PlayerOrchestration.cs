using TecmoTourney.Models;
using TecmoTourney.Orchestration.Interfaces;

namespace TecmoTourney.Orchestration
{
    public class PlayerOrchestration : IPlayerOrchestration
    {
        //Generated Code
        public Task<IEnumerable<PlayerModel>> ListPlayers(int tourneyId)
        {
            throw new NotImplementedException();
        }

        public Task AddPlayerToTournament(int playerId, int tourneyId)
        {
            throw new NotImplementedException();
        }

        public Task RemovePlayerFromTournament(int playerId, int tourneyId)
        {
            throw new NotImplementedException();
        }
        //End Generated Code
    }
}