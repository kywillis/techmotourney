using Microsoft.AspNetCore.Mvc;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;

namespace TecmoTourney.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        [HttpPost("")]
        [ProducesResponseType(201)]
        public async Task<IActionResult> Login([FromBody] AdminLoginRequestModel request)
        {
            if (request.Password == "Browns98")
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = false, // Allow client-side JavaScript access
                    Secure = true, // Only send over HTTPS in production
                    Expires = DateTimeOffset.UtcNow.AddDays(7), // Set the expiration date
                    Path = "/", // The path for which the cookie is valid (root in this case)
                    Domain = null, // Optional: Specify the domain for the cookie
                    SameSite = SameSiteMode.None
                };

                // Add the cookie to the response
                Response.Cookies.Append("AdminAuthToken", "true", cookieOptions);

                return Ok();
            }
            else
            {
                return Unauthorized(new { Error = "Invalid credentials." });
            }
        }
    }
}
