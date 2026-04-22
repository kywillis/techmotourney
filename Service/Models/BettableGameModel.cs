namespace TecmoTourney.Models
{
    /// <summary>Game with lines for wager UI (open for betting or read-only completed / in progress).</summary>
    public class BettableGameModel
    {
        public int GameResultId { get; set; }
        public int TournamentId { get; set; }
        public int Player1Id { get; set; }
        public int Player2Id { get; set; }
        public string Player1Name { get; set; } = string.Empty;
        public string Player2Name { get; set; } = string.Empty;
        public int Player1ProfilePic { get; set; }
        public int Player2ProfilePic { get; set; }
        /// <summary>TC_GameResults.StatusId as enum name (Waiting, InProgress, Completed).</summary>
        public string GameStatus { get; set; } = string.Empty;
        /// <summary>True when the public may place new wagers on this game.</summary>
        public bool IsOpenForBetting { get; set; }
        /// <summary>Final scores for completed games; otherwise null.</summary>
        public int? Player1Score { get; set; }
        public int? Player2Score { get; set; }
        public BettableGameOddsModel Odds { get; set; } = new();
        /// <summary>When set, betting is closed.</summary>
        public DateTime? GameStartedAt { get; set; }
        /// <summary>Who wagered what on this game (when ShowActionOnGames is true).</summary>
        public List<WagerActionItemModel>? Action { get; set; }
        /// <summary>Aggregated pending dollars per side; always populated for liquidity / imbalance UI.</summary>
        public BettableGameMarketDepthModel MarketDepth { get; set; } = new();
    }
}
