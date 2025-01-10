namespace TecmoTourney.DataAccess.Models
{
    public class TournamentBracketUpdateDAOModel
    {
        public int TournamentBracketUpdateId { get; set; }
        public int TournamentId { get; set; }
        public int BracketGameId{ get; set; }
        public int StatusID { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateUpdated { get; set; }
    }
}
