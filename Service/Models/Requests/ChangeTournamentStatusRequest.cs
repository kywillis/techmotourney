namespace TecmoTourney.Models.Requests
{
    public class ChangeTournamentStatusRequest
    {
        public int TournamentId { get; set; }
        public dynamic? BracketData { get; set; }
        public TournamentStatus Status { get; set; }
        public IEnumerable<SaveGameResultRequestModel> NewGames { get; set; } = new List<SaveGameResultRequestModel>();
    }
}
