using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using TecmoTourney.DataAccess.Interfaces;

namespace TecmoTourney.Middleware
{
    /// <summary>
    /// Requires Google JWT + active <see cref="DataAccess.Models.PlayerDAOModel.IsAdmin"/> player for mutating /api/tournaments requests.
    /// GET (and HEAD/OPTIONS) are allowed without auth for public viewing.
    /// </summary>
    public class TournamentsWriteAdminMiddleware
    {
        private readonly RequestDelegate _next;

        public TournamentsWriteAdminMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IPlayerDAO playerDAO)
        {
            if (!context.Request.Path.StartsWithSegments("/api/tournaments", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var method = context.Request.Method;
            if (method.Equals("GET", StringComparison.OrdinalIgnoreCase)
                || method.Equals("HEAD", StringComparison.OrdinalIgnoreCase)
                || method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var auth = await context.AuthenticateAsync("Google");
            if (auth?.Principal == null || !auth.Succeeded)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Unauthorized", errorMessage = "Valid Google sign-in required." });
                return;
            }

            var sub = auth.Principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? auth.Principal.FindFirstValue("sub");
            if (string.IsNullOrEmpty(sub))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Missing sub claim" });
                return;
            }

            var player = await playerDAO.GetPlayerByGoogleSubjectIdAsync(sub);
            if (player == null)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Player not found", code = "not_found" });
                return;
            }

            if (!player.IsActive)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Account not active", code = "pending" });
                return;
            }

            if (!player.IsAdmin)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Admin access required", code = "forbidden" });
                return;
            }

            await _next(context);
        }
    }

    public static class TournamentsWriteAdminMiddlewareExtensions
    {
        public static IApplicationBuilder UseTournamentsWriteAdmin(this IApplicationBuilder app)
        {
            return app.UseMiddleware<TournamentsWriteAdminMiddleware>();
        }
    }
}
