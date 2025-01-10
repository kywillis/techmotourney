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
    }
}
