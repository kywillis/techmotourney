namespace TecmoTourney.Notifications
{
    public interface INtfyClient
    {
        /// <summary>POST plain-text body to ntfy. No-op if topic is not configured.</summary>
        Task SendAsync(string message, CancellationToken cancellationToken = default);
    }
}
