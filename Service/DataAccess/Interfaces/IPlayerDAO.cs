using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.DataAccess.Models;

namespace TecmoTourney.DataAccess.Interfaces
{
    public interface IPlayerDAO
    {
        Task<IEnumerable<PlayerDAOModel>> ListPlayersAsync(int? tourneyId = null, bool includeDeleted = false);
        /// <summary>Non-deleted players with no Google account linked (for admin linking pending signups).</summary>
        Task<IEnumerable<PlayerDAOModel>> ListPlayersEligibleForGoogleLinkAsync();
        /// <summary>Sets GoogleSubjectId and EmailAddress when the row has no Google id yet. Returns false if no row updated.</summary>
        Task<bool> TryLinkGoogleAndEmailAsync(int playerId, string googleSubjectId, string emailAddress);
        Task<PlayerDAOModel?> GetPlayerAsync(int id);
        Task<PlayerDAOModel?> GetPlayerByGoogleSubjectIdAsync(string googleSubjectId);
        Task<PlayerDAOModel> AddPlayerAsync(PlayerDAOModel player);
        Task<PlayerDAOModel> UpdatePlayerAsync(int id, PlayerDAOModel player);
        Task<bool> UpdatePlayerBalanceAsync(int playerId, decimal newBalance);
        Task<bool> SetPlayerGoogleSubjectIdAsync(int playerId, string googleSubjectId);
        Task<bool> DeletePlayerAsync(int id);
    }
}
