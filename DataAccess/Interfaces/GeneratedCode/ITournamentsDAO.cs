using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.DataAccess.Models;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;

namespace TecmoTourney.DataAccess.Interfaces
{
    public interface ITournamentsDAO
    {
        //Generated Code
        Task<Tournament> GetTournamentByIdAsync(int tournamentId);
        Task<IEnumerable<Tournament>> GetTournamentByIdsAsync(int tournamentId, int tournamentId2);
        Task<IEnumerable<Tournament>> GetTournamentByIds2Async(int tournamentId, int tournamentId2);
        //End Generated Code

        Task<IEnumerable<TournamentDAOModel>> ListAllAsync();
        Task<PlayerModel> ListResultsByPlayerAsync(int playerId);
        Task AddTournamentAsync(CreateTournamentRequestModel tournament);
        Task UpdateTournamentAsync(int tournamentId, UpdateTournamentRequestModel tournament);
        Task DeleteTournamentAsync(int tournamentId);
    }
}