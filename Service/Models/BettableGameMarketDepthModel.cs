namespace TecmoTourney.Models
{
    /// <summary>Pending stake totals per side for each market, plus the max imbalance allowed (same as PlaceWager).</summary>
    public class BettableGameMarketDepthModel
    {
        public decimal MaxMarketImbalance { get; set; }
        public decimal SpreadPlayer1 { get; set; }
        public decimal SpreadPlayer2 { get; set; }
        public decimal Over { get; set; }
        public decimal Under { get; set; }
        public decimal MoneyLinePlayer1 { get; set; }
        public decimal MoneyLinePlayer2 { get; set; }
    }
}
