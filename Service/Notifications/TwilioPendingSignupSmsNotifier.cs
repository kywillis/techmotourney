using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace TecmoTourney.Notifications
{
    public class TwilioPendingSignupSmsNotifier : IPendingSignupSmsNotifier
    {
        private readonly TwilioSmsOptions _options;
        private readonly ILogger<TwilioPendingSignupSmsNotifier> _logger;

        public TwilioPendingSignupSmsNotifier(IOptions<TwilioSmsOptions> options, ILogger<TwilioPendingSignupSmsNotifier> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task NotifyNewPendingAsync(int pendingActivationId, string fullName, string email, CancellationToken cancellationToken = default)
        {
            if (!IsConfigured())
            {
                _logger.LogDebug("Twilio SMS skipped: Twilio section missing AccountSid, AuthToken, FromNumber, or NotifyTo.");
                return;
            }

            var body = $"Tecmo wager: new pending signup #{pendingActivationId}. {fullName} <{email}>";

            try
            {
                TwilioClient.Init(_options.AccountSid, _options.AuthToken);
                foreach (var to in GetRecipients())
                {
                    await MessageResource.CreateAsync(
                        to: new PhoneNumber(to),
                        from: new PhoneNumber(_options.FromNumber),
                        body: body).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Twilio SMS failed for pending activation {PendingActivationId}", pendingActivationId);
            }
        }

        private bool IsConfigured() =>
            !string.IsNullOrWhiteSpace(_options.AccountSid)
            && !string.IsNullOrWhiteSpace(_options.AuthToken)
            && !string.IsNullOrWhiteSpace(_options.FromNumber)
            && !string.IsNullOrWhiteSpace(_options.NotifyTo);

        private IEnumerable<string> GetRecipients() =>
            _options.NotifyTo.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(static s => !string.IsNullOrWhiteSpace(s));
    }
}
