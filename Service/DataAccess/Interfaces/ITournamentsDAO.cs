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
        Task<PlayerModel?> ListResultsByPlayerAsync(int playerId);
        Task<TournamentDAOModel?> GetById(int tournamentId);
        Task UpdateTournamentStatusAsync(int tournamentId, int statusId);
        Task UpdateTournamentBracketDataAsync(int tournamentId, string bracketData);
        Task<TournamentDAOModel> AddTournamentAsync(UpdateTournamentRequestModel tournament);
        Task<TournamentDAOModel> UpdateTournamentAsync(int tournamentId, UpdateTournamentRequestModel tournament);
        Task DeleteTournamentAsync(int tournamentId);
    }
}
