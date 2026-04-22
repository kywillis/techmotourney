using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.ResultPattern;

namespace TecmoTourney.Orchestration.Interfaces
{
    public interface ITournamentsOrchestration
    {
        Task<Operation<List<TournamentModel>, ApiError>> ListAllAsync();
        Task<Operation<TournamentModel, ApiError>> AddTournamentAsync(UpdateTournamentRequestModel tournament);
        Task<Operation<TournamentModel, ApiError>> GetActive();
        Task<Operation<TournamentModel, ApiError>> GetById(int tournamentId);
        Task<Operation<TournamentModel, ApiError>> UpdateTournamentAsync(int tournamentId, UpdateTournamentRequestModel tournament);
        Task<Operation<bool, ApiError>> DeleteTournamentAsync(int tournamentId);
        Task<Operation<ChangeTournamentStatusResponseModel, ApiError>> ChangeStatusAsync(ChangeTournamentStatusRequest request);
        Task<Operation<TournamentModel, ApiError>> UpdateBracketDataAsync(int tournamentId, string bracketData);
        Task<Operation<List<TournamentStandingModel>, ApiError>> GetStandingsAsync(int tournamentId, TournamentStatus status);

        Task<Operation<bool, ApiError>> Reset(int tournamentId, ResetTournamentRequestModel model);

        /// <summary>Removes bracket-phase games, their wagers and odds, clears bracket JSON, sets status to Preliminaries. Prelims unchanged.</summary>
        Task<Operation<bool, ApiError>> ResetTournamentPhaseAsync(int tournamentId, ResetTournamentRequestModel model);

        Task<Operation<RecalculateBracketResponseModel, ApiError>> RecalculateBracketAsync(int tournamentId);
    }
}
