namespace TecmoTourney
{
    /// <summary>
    /// Twilio SMS for admin alerts (e.g. new wager pending signups). Leave credentials empty to disable.
    /// Put secrets in appsettings.secrets.json or environment variables.
    /// </summary>
    public class TwilioSmsOptions
    {
        public const string SectionName = "Twilio";

        public string AccountSid { get; set; } = string.Empty;
        public string AuthToken { get; set; } = string.Empty;
        /// <summary>E.164 sender number from Twilio (e.g. +15551234567).</summary>
        public string FromNumber { get; set; } = string.Empty;
        /// <summary>Recipient(s) in E.164; comma or semicolon separated for multiple numbers.</summary>
        public string NotifyTo { get; set; } = string.Empty;
    }
}
