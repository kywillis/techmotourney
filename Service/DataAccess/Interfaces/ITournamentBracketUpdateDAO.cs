using TecmoTourney.DataAccess.Models;

namespace TecmoTourney.DataAccess.Interfaces
{
    public interface ITournamentBracketUpdateDAO
    {
        Task<IEnumerable<TournamentBracketUpdateDAOModel>> GetByTournamentIdAsync(int tournamentId, int statusId);
        Task<TournamentBracketUpdateDAOModel> GetByUpdateIdAsync(int tournamentBracketUpdateId);
        Task<TournamentBracketUpdateDAOModel> Save(TournamentBracketUpdateDAOModel update);
    }
}
