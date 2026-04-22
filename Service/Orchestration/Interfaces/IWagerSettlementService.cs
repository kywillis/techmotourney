using System.Threading.Tasks;
using TecmoTourney.DataAccess.Models;

namespace TecmoTourney.Orchestration.Interfaces
{
    /// <summary>
    /// Grades and settles wagers when a game result is saved (single entry point from <see cref="IGameResultOrchestration"/>).
    /// </summary>
    public interface IWagerSettlementService
    {
        /// <summary>
        /// After bracket reconciliation: reverse prior settlement if needed, then grade all non-cancelled wagers for this game.
        /// No-ops unless the game is completed, has a winner (not tied, winner score &gt; 0), and odds exist.
        /// </summary>
        Task SettleWagersAfterGameSaveAsync(GameResultDAOModel game);
    }
}
