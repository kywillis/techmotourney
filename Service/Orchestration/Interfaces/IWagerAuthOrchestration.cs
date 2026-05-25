using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.ResultPattern;

namespace TecmoTourney.Orchestration.Interfaces
{
    public interface IWagerAuthOrchestration
    {
        Task<Operation<WagerAuthResponseModel, ApiError>> AuthenticateAsync(WagerAuthRequestModel request);
    }
}
