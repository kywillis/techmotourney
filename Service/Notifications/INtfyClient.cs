namespace TecmoTourney.Notifications
{
    public interface INtfyClient
    {
        /// <summary>POST plain-text body to ntfy. No-op if topic is not configured.</summary>
        /// <param name="title">ntfy title (X-Title). Omit for untitled message.</param>
        Task SendAsync(string message, string? title = null, CancellationToken cancellationToken = default);
    }
}
