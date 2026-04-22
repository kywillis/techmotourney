using TecmoTourney.Models;
using TecmoTourney.ResultPattern;

namespace TecmoTourney.Orchestration.Interfaces;

public interface ITournamentBracketReconciliationService
{
    Task<Operation<RecalculateBracketResponseModel, ApiError>> ReconcileAsync(
        int tournamentId,
        IReadOnlyList<TournamentStandingModel> standings);
}
