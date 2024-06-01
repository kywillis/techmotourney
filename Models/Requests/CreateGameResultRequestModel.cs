namespace TecmoTourney.Models.Requests
{
    public class CreateGameResultRequestModel
    {
        public GameResultStatsModel Player1 { get; set; }
        public GameResultStatsModel Player2 { get; set; }
        public int TournamentId { get; set; }
    }
}
