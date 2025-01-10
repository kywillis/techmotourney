using TecmoTourney.Models;
using TecmoTourney.ResultPattern;

namespace TecmoTourney.Orchestration.Interfaces
{
    public interface IGameTeamOrchestration
    {
        public Task<Operation<List<GameTeamModel>, ApiError>> GetAll(); 
    }
}
