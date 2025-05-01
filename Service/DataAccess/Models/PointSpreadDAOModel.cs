namespace TecmoTourney.DataAccess.Models
{
    public class PointSpreadDAOModel
    {
        public int PointSpreadId { get; set; }
        public int TournamentId { get; set; }
        public int Player1ID { get; set; }
        public int Player2ID { get; set; }
        public int BracketTypeId { get; set; }
        public int Spread { get; set; }
        public int? FavoredPlayerId { get; set; } = null;
        public string Summary { get; set; } = string.Empty;
    }
}
