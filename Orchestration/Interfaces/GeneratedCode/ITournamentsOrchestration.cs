using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;

namespace TecmoTourney.Orchestration.Interfaces
{
    public interface ITournamentsOrchestration
    {
        Task<IEnumerable<TournamentModel>> ListAllAsync();
        Task<PlayerModel> ListResultsByPlayerAsync(int playerId);
        Task AddTournamentAsync(CreateTournamentRequestModel tournament);
        Task UpdateTournamentAsync(int tournamentId, UpdateTournamentRequestModel tournament);
        Task DeleteTournamentAsync(int tournamentId);

        //Generated Code
        Task<TournamentModel> GetTournamentByIdAsync(int tournamentId);
        //End Generated Code
    }
}