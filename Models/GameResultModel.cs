namespace TecmoTourney.Models
{
    public class GameResultModel
    {
        public int GameResultId { get; set; }
        public GameResultStatsModel Player1 { get; set; }
        public GameResultStatsModel Player2 { get; set; }
        public int TournamentId { get; set; }
    }
}
