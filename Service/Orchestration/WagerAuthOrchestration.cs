using System.Net;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using TecmoTourney.DataAccess.Interfaces;
using TecmoTourney.DataAccess.Models;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.Notifications;
using TecmoTourney.Orchestration.Interfaces;
using TecmoTourney.ResultPattern;

namespace TecmoTourney.Orchestration
{
    public class WagerAuthOrchestration : IWagerAuthOrchestration
    {
        private readonly IPlayerDAO _playerDAO;
        private readonly IPendingActivationDAO _pendingActivationDAO;
        private readonly GoogleAuthOptions _googleAuthOptions;
        private readonly INtfyClient _ntfy;

        public WagerAuthOrchestration(
            IPlayerDAO playerDAO,
            IPendingActivationDAO pendingActivationDAO,
            IOptions<GoogleAuthOptions> googleAuthOptions,
            INtfyClient ntfy)
        {
            _playerDAO = playerDAO;
            _pendingActivationDAO = pendingActivationDAO;
            _googleAuthOptions = googleAuthOptions.Value;
            _ntfy = ntfy;
        }

        public async Task<Operation<WagerAuthResponseModel, ApiError>> AuthenticateAsync(WagerAuthRequestModel request)
        {
            if (string.IsNullOrWhiteSpace(request.IdToken))
                return new ApiError("IdToken is required", HttpStatusCode.BadRequest);

            if (string.IsNullOrWhiteSpace(_googleAuthOptions.ClientId))
                return new ApiError("Google authentication is not configured (missing ClientId).", HttpStatusCode.ServiceUnavailable);

            GoogleJsonWebSignature.Payload? payload;
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _googleAuthOptions.ClientId }
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
            }
            catch (InvalidJwtException ex)
            {
                return new ApiError($"Invalid Google token: {ex.Message}", HttpStatusCode.Unauthorized);
            }

            var googleSubjectId = payload.Subject;
            var email = payload.Email ?? "";
            var fullName = payload.Name ?? (payload.GivenName + " " + payload.FamilyName).Trim();
            if (string.IsNullOrWhiteSpace(fullName))
                fullName = email;

            var player = await _playerDAO.GetPlayerByGoogleSubjectIdAsync(googleSubjectId);
            if (player != null)
            {
                if (!player.IsActive)
                {
                    var pending = await _pendingActivationDAO.GetByGoogleSubjectIdAsync(googleSubjectId);
                    return new WagerAuthResponseModel
                    {
                        IsAuthenticated = false,
                        IsPending = true,
                        Message = "Your account is waiting to be activated.",
                        PendingActivationId = pending?.PendingActivationId,
                        Email = pending?.Email ?? email,
                        RequestedProfilePic = pending?.RequestedProfilePic ?? 0
                    };
                }
                return new WagerAuthResponseModel
                {
                    IsAuthenticated = true,
                    IsPending = false,
                    PlayerId = player.PlayerId,
                    FullName = player.FullName,
                    IsAdmin = player.IsAdmin,
                    Balance = player.Balance,
                    ProfilePic = player.ProfilePic > 0 ? player.ProfilePic : null
                };
            }

            var pendingActivation = await _pendingActivationDAO.GetByGoogleSubjectIdAsync(googleSubjectId);
            if (pendingActivation != null)
            {
                return new WagerAuthResponseModel
                {
                    IsAuthenticated = false,
                    IsPending = true,
                    Message = "Your account is waiting to be activated.",
                    PendingActivationId = pendingActivation.PendingActivationId,
                    Email = pendingActivation.Email,
                    RequestedProfilePic = pendingActivation.RequestedProfilePic
                };
            }

            var firstName = payload.GivenName ?? fullName.Split(' ').FirstOrDefault() ?? "there";
            var newPending = new PendingActivationDAOModel
            {
                GoogleSubjectId = googleSubjectId,
                Email = email,
                FullName = fullName,
                RequestedProfilePic = 0,
                Status = PendingActivationStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };
            await _pendingActivationDAO.CreateAsync(newPending);
            await _ntfy.SendAsync(
                $"Wager signup pending: {newPending.FullName} ({newPending.Email})  id {newPending.PendingActivationId}",
                "Pending Sign Up");
            return new WagerAuthResponseModel
            {
                IsAuthenticated = false,
                IsPending = true,
                Message = $"Welcome {firstName}, your account is waiting to be activated by Willis.",
                PendingActivationId = newPending.PendingActivationId,
                Email = newPending.Email,
                RequestedProfilePic = newPending.RequestedProfilePic
            };
        }
    }
}
