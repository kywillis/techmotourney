namespace TecmoTourney.Notifications
{
    public interface IPendingSignupSmsNotifier
    {
        /// <summary>Sends admin SMS when a new TC_PendingActivations row is created. Swallows errors after logging.</summary>
        Task NotifyNewPendingAsync(int pendingActivationId, string fullName, string email, CancellationToken cancellationToken = default);
    }
}
