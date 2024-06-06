namespace TecmoTourney.DataAccess.Models
{
    public class TournamentDAOModel
    {
        public int TournamentId { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
