using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using TecmoTourney.DataAccess.Interfaces;

namespace TecmoTourney.Middleware
{
    /// <summary>
    /// For wager API requests that are authenticated with the Google scheme, resolves the "sub" claim
    /// to the current PlayerId and IsAdmin and sets them in HttpContext.Items so controllers can use them.
    /// Skips the auth endpoint (POST /api/wager/auth/google) and unauthenticated requests.
    /// </summary>
    public class WagerPlayerResolutionMiddleware
    {
        private readonly RequestDelegate _next;
        private const string WagerPlayerIdKey = "WagerPlayerId";
        private const string WagerIsAdminKey = "WagerIsAdmin";

        public static string WagerPlayerIdItemKey => WagerPlayerIdKey;
        public static string WagerIsAdminItemKey => WagerIsAdminKey;

        public WagerPlayerResolutionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IPlayerDAO playerDAO)
        {
            if (!context.Request.Path.StartsWithSegments("/api/wager", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }
            if (context.Request.Method == "POST" && context.Request.Path.StartsWithSegments("/api/wager/auth", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var result = await context.AuthenticateAsync("Google");
            if (result?.Principal == null || !result.Succeeded)
            {
                await _next(context);
                return;
            }

            var sub = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? result.Principal.FindFirstValue("sub");
            if (string.IsNullOrEmpty(sub))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { error = "Missing sub claim" });
                return;
            }

            var player = await playerDAO.GetPlayerByGoogleSubjectIdAsync(sub);
            if (player == null)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { error = "Player not found", code = "not_found" });
                return;
            }
            if (!player.IsActive)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { error = "Account not active", code = "pending" });
                return;
            }

            context.Items[WagerPlayerIdKey] = player.PlayerId;
            context.Items[WagerIsAdminKey] = player.IsAdmin;
            await _next(context);
        }
    }

    public static class WagerPlayerResolutionMiddlewareExtensions
    {
        public static IApplicationBuilder UseWagerPlayerResolution(this IApplicationBuilder app)
        {
            return app.UseMiddleware<WagerPlayerResolutionMiddleware>();
        }
    }

    public static class WagerHttpContextExtensions
    {
        public static int? GetWagerPlayerId(this HttpContext context)
        {
            return context.Items[WagerPlayerResolutionMiddleware.WagerPlayerIdItemKey] as int?;
        }

        public static bool GetWagerIsAdmin(this HttpContext context)
        {
            return context.Items[WagerPlayerResolutionMiddleware.WagerIsAdminItemKey] as bool? ?? false;
        }
    }
}
