using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.DataAccess.Models;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;

namespace TecmoTourney.DataAccess.Interfaces
{
    public interface ITournamentsDAO
    {
        Task<IEnumerable<TournamentDAOModel>> ListAllAsync();
        Task<PlayerModel> ListResultsByPlayerAsync(int playerId);
        Task AddTournamentAsync(CreateTournamentRequestModel tournament);
        Task UpdateTournamentAsync(int tournamentId, UpdateTournamentRequestModel tournament);
        Task DeleteTournamentAsync(int tournamentId);

        //Generated Code
        Task<TournamentModel> GetTournamentByIdAsync(int tournamentId);
        Task<IEnumerable<TournamentModel>> GetTournamentByIdsAsync(int tournamentId, int tournamentId2);
        Task<IEnumerable<TournamentModel>> GetTournamentByIfffAsync(int tournamentId, int tournamentId2);
        //End Generated Code
    }
}