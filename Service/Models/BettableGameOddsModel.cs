namespace TecmoTourney.Models
{
    /// <summary>Odds for a bettable game (spread in half-points, O/U, ML).</summary>
    public class BettableGameOddsModel
    {
        public decimal Spread { get; set; }
        public int? FavoredPlayerId { get; set; }
        public decimal? OverUnder { get; set; }
        public decimal? MoneyLinePlayer1 { get; set; }
        public decimal? MoneyLinePlayer2 { get; set; }
        /// <summary>Optional LLM / analyst write-up when present.</summary>
        public string Summary { get; set; } = string.Empty;
    }
}
