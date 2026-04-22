namespace TecmoTourney.Models
{
    /// <summary>Read-only odds and market depth for a game; public, no authentication.</summary>
    public class PublicWageringSnapshotModel
    {
        public int GameResultId { get; set; }
        public int Player1Id { get; set; }
        public int Player2Id { get; set; }
        public string Player1Name { get; set; } = string.Empty;
        public string Player2Name { get; set; } = string.Empty;
        public int Player1ProfilePic { get; set; }
        public int Player2ProfilePic { get; set; }
        public PublicWageringOddsModel Odds { get; set; } = new();
        public BettableGameMarketDepthModel MarketDepth { get; set; } = new();
    }

    public class PublicWageringOddsModel
    {
        public decimal Spread { get; set; }
        public int? FavoredPlayerId { get; set; }
        public decimal? OverUnder { get; set; }
        public decimal? MoneyLinePlayer1 { get; set; }
        public decimal? MoneyLinePlayer2 { get; set; }
        public string Summary { get; set; } = string.Empty;
    }
}
