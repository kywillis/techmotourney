using System.Collections.Generic;
using System.Threading.Tasks;

namespace TecmoTourney.Orchestration.Interfaces
{
    /// <summary>When a game (or odds) is removed, pending wagers are cancelled and refunded; settled wagers stay as history with GameResultId cleared.</summary>
    public interface IWagerDetachmentService
    {
        Task DetachWagersForGameResultAsync(int gameResultId, int? actorPlayerId = null);
        Task DetachWagersForGameResultsAsync(IEnumerable<int> gameResultIds, int? actorPlayerId = null);
    }
}
