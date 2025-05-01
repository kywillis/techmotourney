namespace TecmoTourney.Models
{
    public class PointSpreadModel
    {
        public int TournamentId { get; set; }
        public int Player1Id { get; set; }
        public int Player2Id { get; set; }
        public BracketLocation BracketType { get; set; }
        public double Spread { get; set; }
        public int FavoredPlayerId { get; set; }
    }
}
