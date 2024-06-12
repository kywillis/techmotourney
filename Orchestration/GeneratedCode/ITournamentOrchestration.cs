using System.Collections.Generic;
using System.Threading.Tasks;
using SE.Zeigo.Admin.Models;

namespace SE.Zeigo.Admin.Orchestration
{
    public interface ITournamentOrchestration
    {
        Task<IEnumerable<Tournament>> ListAll();
        Task<Player> ListResultsByPlayer(int playerId);
        Task AddTournament(CreateTournamentRequest tournament);
        Task UpdateTournament(int tournamentId, UpdateTournamentRequest tournament);
        Task DeleteTournament(int tournamentId);
        Task<Tournament> GetTournamentById(int tournamentId);
        Task<IEnumerable<Tournament>> GetTournamentByIds(int tournamentId, int tournamentId2);
    }
}