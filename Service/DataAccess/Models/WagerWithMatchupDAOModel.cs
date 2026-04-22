using TecmoTourney;

namespace TecmoTourney.DataAccess.Models
{
    /// <summary>TC_Wagers row with matchup names from game + players.</summary>
    public class WagerWithMatchupDAOModel
    {
        public int WagerId { get; set; }
        public int PlayerId { get; set; }
        public int? GameResultId { get; set; }
        public int TournamentId { get; set; }
        public WagerMarketType MarketType { get; set; }
        public WagerSide Side { get; set; }
        public decimal StakeAmount { get; set; }
        public WagerStatus Status { get; set; } = WagerStatus.Pending;
        public DateTime CreatedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? SettledAt { get; set; }
        public string Player1Name { get; set; } = string.Empty;
        public string Player2Name { get; set; } = string.Empty;
        public int MatchPlayer1Id { get; set; }
        public int MatchPlayer2Id { get; set; }
        public decimal OddsSpread { get; set; }
        public int? OddsFavoredPlayerId { get; set; }
        public decimal? OddsMoneyLinePlayer1 { get; set; }
        public decimal? OddsMoneyLinePlayer2 { get; set; }
        public decimal? OddsOverUnder { get; set; }
    }
}
