namespace TecmoTourney.Models
{
    public class GameStationGamesResponseModel
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; } = string.Empty;
        public List<GameResultModel> Waiting { get; set; } = new();
        public List<GameResultModel> InProgress { get; set; } = new();
    }
}
