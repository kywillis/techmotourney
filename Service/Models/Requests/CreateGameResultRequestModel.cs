namespace TecmoTourney.Models.Requests
{
    public class SaveGameResultRequestModel
    {
        public int? GameResultId { get; set; }
        public GameResultStatsModel Player1 { get; set; } = new GameResultStatsModel();
        public GameResultStatsModel Player2 { get; set; } = new GameResultStatsModel();
        public int TournamentId { get; set; }
        public GameStatus Status { get; set; }
        public GameType GameType { get; set; }
        public int BracketGameId { get; set; }

        /// <summary>Optional audit: caller identity (e.g. score-grabber).</summary>
        public string? SaveSource { get; set; }

        /// <summary>Optional client correlation id for logs.</summary>
        public string? ClientCorrelationId { get; set; }

        /// <summary>When true and Status is Completed, equal scores are allowed (Tecmo tie).</summary>
        public bool AllowTieScore { get; set; }

        /// <summary>When true, incoming passing/rushing yards are added to existing DB values (rematch after tie).</summary>
        public bool AccumulateStatsFromTieLeg { get; set; }
    }
}
