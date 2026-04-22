using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TecmoTourney.Notifications
{
    public class NtfyClient : INtfyClient
    {
        private readonly HttpClient _http;
        private readonly NtfyOptions _options;
        private readonly ILogger<NtfyClient> _logger;

        public NtfyClient(HttpClient http, IOptions<NtfyOptions> options, ILogger<NtfyClient> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendAsync(string message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.Topic) || string.IsNullOrEmpty(message))
                return;

            var baseUrl = _options.BaseUrl?.Trim() ?? "https://ntfy.sh";
            if (!baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                !baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                baseUrl = "https://" + baseUrl;

            var topic = _options.Topic.Trim();
            var requestUri = $"{baseUrl.TrimEnd('/')}/{Uri.EscapeDataString(topic)}";

            try
            {
                using var content = new StringContent(message, Encoding.UTF8, "text/plain");
                var response = await _http.PostAsync(requestUri, content, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("ntfy returned {Code}: {Reason}", (int)response.StatusCode, response.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ntfy request failed");
            }
        }
    }
}
