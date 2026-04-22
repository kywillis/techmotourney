namespace TecmoTourney.Models
{
    public class GameOddsModel
    {
        public int? GameResultId { get; set; }
        public int TournamentId { get; set; }
        public int Player1Id { get; set; }
        public int Player2Id { get; set; }
        public BracketLocation BracketType { get; set; }
        public decimal Spread { get; set; }
        public int FavoredPlayerId { get; set; }
        public decimal? MoneyLinePlayer1 { get; set; }
        public decimal? MoneyLinePlayer2 { get; set; }
        public decimal? OverUnder { get; set; }
    }
}
