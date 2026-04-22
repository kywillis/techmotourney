namespace TecmoTourney
{
    public class NtfyOptions
    {
        public const string SectionName = "Ntfy";

        public string BaseUrl { get; set; } = "https://ntfy.sh";

        /// <summary>Subscribable topic (secret token). If empty, ntfy calls are disabled.</summary>
        public string Topic { get; set; } = string.Empty;
    }
}
