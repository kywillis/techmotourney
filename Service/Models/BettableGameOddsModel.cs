namespace TecmoTourney.Models
{
    /// <summary>Odds for a bettable game (spread in half-points, O/U, ML).</summary>
    public class BettableGameOddsModel
    {
        public decimal Spread { get; set; }
        public int? FavoredPlayerId { get; set; }
        public decimal? OverUnder { get; set; }
        public int? MoneyLinePlayer1 { get; set; }
        public int? MoneyLinePlayer2 { get; set; }
    }
}
