namespace TecmoTourney.Models
{
    public class TournamentModel
    {
        public int TournamentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string BracketData { get; set; } = string.Empty;
        public string BracketImage { get; set; } = string.Empty;
        public TournamentStatus Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
