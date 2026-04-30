namespace TecmoTourney
{
    public class ApplicationConfig
    {
        public string MainDBConnectionString { get; set; } = string.Empty;

        /// <summary>House vig on winning wagers, percent of profit (5 = 5%). 0 = none.</summary>
        public int WageringVigPercent { get; set; }
    }
}
