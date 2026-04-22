using TecmoTourney.DataAccess.Models;
using TecmoTourney.Models;

namespace TecmoTourney.Orchestration.Interfaces
{
    /// <summary>
    /// LLM-based odds generation and persistence; odds rows always include <see cref="GameOddsDAOModel.GameResultId"/>.
    /// </summary>
    public interface IGameOddsGenerationService
    {
        /// <summary>
        /// For each saved game, inserts TC_GameOdds with GameResultId when missing. One LLM call for the batch.
        /// Games are already persisted; failures here do not roll back games. Best-effort inserts on total failure.
        /// </summary>
        Task<OddsGenerationStatusModel> EnsureOddsForNewGameResultsAsync(
            IReadOnlyList<GameResultDAOModel> savedGamesWithIds,
            CancellationToken cancellationToken = default);
    }
}
