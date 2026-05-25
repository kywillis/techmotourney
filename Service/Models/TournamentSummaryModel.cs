namespace TecmoTourney.Models
{
    /// <summary>W-L and net $ for the current user in a tournament (from wager audit).</summary>
    public class TournamentSummaryModel
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; } = string.Empty;
        public int Wins { get; set; }
        public int Losses { get; set; }
        public decimal NetAmount { get; set; }
    }
}
