using TecmoTourney.DataAccess.Models;

namespace TecmoTourney.DataAccess.Interfaces
{
    public interface ITournamentBracketUpdateDAO
    {
        Task<IEnumerable<TournamentBracketUpdateDAOModel>> GetByTournamentIdAsync(int tournamentId);
        Task<TournamentBracketUpdateDAOModel> Save(TournamentBracketUpdateDAOModel update);
    }
}
