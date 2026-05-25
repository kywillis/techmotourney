namespace TecmoTourney
{
    /// <summary>
    /// Google OAuth 2.0 / Sign-In client configuration for wager app.
    /// Set ClientId to the Web client ID from Google Cloud Console (same as used by the Angular wager app).
    /// </summary>
    public class GoogleAuthOptions
    {
        public const string SectionName = "GoogleAuth";

        public string ClientId { get; set; } = string.Empty;
    }
}
