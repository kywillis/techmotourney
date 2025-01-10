using System.Numerics;
using TecmoTourney.Models;
using TecmoTourney.ResultPattern;

namespace TecmoTourney.Orchestration.Interfaces
{
    public interface IPlayerOrchestration
    {
        Task<Operation<List<PlayerModel>, ApiError>> GetPlayersAsync(int tournamentId);
        Task<Operation<List<PlayerModel>, ApiError>> GetAllPlayersAsync();

        Task<Operation<PlayerModel, ApiError>> GetPlayerAsync(int playerId);
        Task<Operation<PlayerModel, ApiError>> CreatePlayerAsync(CreatePlayerRequestModel player);
        Task<Operation<PlayerModel, ApiError>> UpdatePlayerAsync(int playerId, PlayerModel player);
        Task<Operation<PlayerModel, ApiError>> DeletePlayerAsync(int playerId);
        Task<Operation<bool, ApiError>> AddPlayerToTournament(int playerId, int tournamentId);
        Task<Operation<bool, ApiError>> RemovePlayerFromTournament(int playerId, int tournamentId);
    }
}
