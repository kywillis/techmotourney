using System.Text.Json.Serialization;
using TecmoTourney;

namespace TecmoTourney.Models
{
    public class WagerModel
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
        /// <summary>Populated for list views (my wagers).</summary>
        [JsonPropertyName("player1Name")]
        public string Player1Name { get; set; } = string.Empty;

        [JsonPropertyName("player2Name")]
        public string Player2Name { get; set; } = string.Empty;

        /// <summary>Display line for my-wagers list (e.g. Sinagra (spread +3)).</summary>
        [JsonPropertyName("pickDescription")]
        public string PickDescription { get; set; } = string.Empty;

        /// <summary>Total dollars returned if the wager wins (stake + profit).</summary>
        [JsonPropertyName("potentialPayout")]
        public decimal PotentialPayout { get; set; }

        [JsonIgnore]
        public int MatchPlayer1Id { get; set; }

        [JsonIgnore]
        public int MatchPlayer2Id { get; set; }

        [JsonIgnore]
        public decimal OddsSpread { get; set; }

        [JsonIgnore]
        public int? OddsFavoredPlayerId { get; set; }

        [JsonIgnore]
        public decimal? OddsMoneyLinePlayer1 { get; set; }

        [JsonIgnore]
        public decimal? OddsMoneyLinePlayer2 { get; set; }

        [JsonIgnore]
        public decimal? OddsOverUnder { get; set; }

        /// <summary>Admin pending-wagers list only.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("bettorFullName")]
        public string? BettorFullName { get; set; }
    }
}
