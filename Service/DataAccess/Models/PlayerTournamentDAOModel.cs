namespace TecmoTourney.DataAccess.Models
{
    public class PlayerTournamentDAOModel
    {
        public int PlayerTournamentId { get; set; }
        public int PlayerId { get; set; }
        public int TournamentId { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateModified { get; set; }
    }
}
