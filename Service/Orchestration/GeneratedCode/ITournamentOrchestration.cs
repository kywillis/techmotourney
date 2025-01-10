using System.Collections.Generic;
using System.Threading.Tasks;
using SE.Zeigo.Admin.Models;

namespace SE.Zeigo.Admin.Orchestration
{
    public interface ITournamentOrchestration
    {
        Task<IEnumerable<Tournament>> ListAllAsync();
        Task<Player> ListResultsByPlayerAsync(int playerId);
        Task AddTournamentAsync(CreateTournamentRequest tournament);
        Task UpdateTournamentAsync(int tournamentId, UpdateTournamentRequest tournament);
        Task DeleteTournamentAsync(int tournamentId);
        Task<Tournament> GetTournamentByIdAsync(int tournamentId);
        Task<IEnumerable<Tournament>> GetTournamentByIdsAsync(int tournamentId, int tournamentId2);
        Task<IEnumerable<Tournament>> GetTournamentByIfffAsync(int tournamentId, int tournamentId2);
    }
}