namespace TecmoTourney.DataAccess.Models
{
    public class TournamentDAOModel
    {
        public int TournamentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int StatusId { get; set; }
        public string BracketData { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
