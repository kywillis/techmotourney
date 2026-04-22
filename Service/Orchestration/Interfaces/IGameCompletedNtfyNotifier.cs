using TecmoTourney.Models;

namespace TecmoTourney.Orchestration.Interfaces
{
    public interface IGameCompletedNtfyNotifier
    {
        /// <summary>After save + settlement, when the game has just become Completed.</summary>
        Task TryNotifyFirstCompletedAsync(
            GameResultModel game,
            int gameResultId,
            int tournamentId,
            CancellationToken cancellationToken = default);
    }
}
