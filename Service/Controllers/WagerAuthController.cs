using Microsoft.AspNetCore.Mvc;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.Orchestration.Interfaces;
using TecmoTourney.ResultPattern;

namespace TecmoTourney.Controllers
{
    [ApiController]
    [Route("api/wager/auth")]
    public class WagerAuthController : ControllerBase
    {
        private readonly IWagerAuthOrchestration _wagerAuthOrchestration;

        public WagerAuthController(IWagerAuthOrchestration wagerAuthOrchestration)
        {
            _wagerAuthOrchestration = wagerAuthOrchestration;
        }

        [HttpPost("google")]
        [ProducesResponseType(200, Type = typeof(WagerAuthResponseModel))]
        [ProducesResponseType(400)]
        [ProducesResponseType(501)]
        public async Task<IActionResult> Authenticate([FromBody] WagerAuthRequestModel request)
        {
            var result = await _wagerAuthOrchestration.AuthenticateAsync(request);
            return result.ToActionResult();
        }
    }
}
