using System.Threading.Tasks;
using SE.Zeigo.Admin.Models;

namespace SE.Zeigo.Admin.Orchestration
{
    public interface ITournamentsOrchestration
    {
        Task<Tournament[]> ListAll();
        Task<Player> ListResultsByPlayer(int playerId);
        Task AddTournament(CreateTournamentRequest tournament);
        Task UpdateTournament(int tournamentId, UpdateTournamentRequest tournament);
        Task DeleteTournament(int tournamentId);
        Task<Tournament> GetTournamentById(int tournamentId);
        Task<Tournament[]> GetTournamentByIds(int tournamentId, int tournamentId2);
    }
}