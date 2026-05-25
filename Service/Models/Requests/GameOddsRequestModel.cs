namespace TecmoTourney.Models
{
    public class GameOddsRequestModel
    {
        public int TournamentId { get; set; }
        public int Player1ID { get; set; }
        public int Player2ID { get; set; }
        public BracketLocation BracketType { get; set; }
    }
}
