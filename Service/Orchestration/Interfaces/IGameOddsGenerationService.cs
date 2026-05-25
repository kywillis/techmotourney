using TecmoTourney.DataAccess.Models;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.ResultPattern;

namespace TecmoTourney.Orchestration.Interfaces
{
    /// <summary>
    /// Single place for LLM-based odds generation and persistence (linked to <see cref="GameResultDAOModel"/> when applicable).
    /// </summary>
    public interface IGameOddsGenerationService
    {
        /// <summary>
        /// For each saved game with a GameResultId, inserts TC_GameOdds with GameResultId set if none exists yet.
        /// Batches all games into one LLM call when possible.
        /// </summary>
        Task EnsureOddsForNewGameResultsAsync(IReadOnlyList<GameResultDAOModel> savedGamesWithIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Legacy endpoint: create odds from requests (no GameResultId). Skips matchups that already exist for the tournament + bracket.
        /// </summary>
        Task<Operation<List<GameOddsModel>, ApiError>> CreateOddsFromRequestsAsync(int tournamentId, IEnumerable<GameOddsRequestModel> pointSpreads, CancellationToken cancellationToken = default);
    }
}
