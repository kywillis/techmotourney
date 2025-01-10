namespace TecmoTourney.Models
{
    public class TournamentModel
    {
        public int TournamentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public TournamentBracketModel TournamentBracket { get; set; } = new TournamentBracketModel();
        public TournamentStatus Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
