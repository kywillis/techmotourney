namespace TecmoTourney.DataAccess.Models
{
    /// <summary>Pending wagers for a tournament with matchup + bettor display name.</summary>
    public class AdminPendingWagerRowDAOModel : WagerWithMatchupDAOModel
    {
        public string BettorFullName { get; set; } = string.Empty;
    }
}
