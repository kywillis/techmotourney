namespace TecmoTourney.Models
{
    public class GameResultModel
    {
        public int GameResultId { get; set; }
        public GameResultStatsModel Player1 { get; set; } = new GameResultStatsModel();
        public GameResultStatsModel Player2 { get; set; } = new GameResultStatsModel();
        public int TournamentId { get; set; }
        public GameStatus Status { get; set; }
        public GameType GameType { get; set; }
        public int BracketGameId { get; set; }
        public int MatchUpIndex { get; set; }
        public DateTime Date { get; set; }
        /// <summary>If set, this game does not count toward this player's preliminary seeding.</summary>
        public int? SeedingExemptPlayerId { get; set; }
    }
}
