using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.ResultPattern;

namespace TecmoTourney.Orchestration.Interfaces
{
    public interface IGameResultOrchestration
    {
        Task<Operation<List<GameResultModel>, ApiError>> ListResultsByTournamentAsync(int tournamentId, bool includeDeledted = false);
        Task<Operation<List<GameResultModel>, ApiError>> ListResultsByPlayerAsync(int playerId);
        Task<Operation<List<GameResultModel>, ApiError>> ListResultsByBracketGameIDsAsync(IEnumerable<int> bracketGameIds);
        Task<Operation<List<GameResultModel>, ApiError>> SearchAsync(int? tournamentId, int? player1Id, int? player2Id, BracketLocation? bracketLocation);
        Task<Operation<GameResultModel, ApiError>> GetById(int gameResultId);
        Task<Operation<SaveGameResultResponseModel, ApiError>> SaveGameResultAsync(SaveGameResultRequestModel gameResult);
        Task<Operation<bool, ApiError>> DeleteGameResultAsync(int id);
        Task<Operation<List<TournamentBracketUpdateModel>, ApiError>> GetGameUpdates(int tournamentId);
        Task<Operation<bool, ApiError>> AcknowledgeBracketUpdate(int tournamentBracketUpdateId);
        Task<Operation<List<GameOddsModel>, ApiError>> GetPointSpreadsAsync(int tournamentId);
    }
}
