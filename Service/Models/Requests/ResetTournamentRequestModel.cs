namespace TecmoTourney.Models.Requests
{
    public class ResetTournamentRequestModel
    {
        public string Password { get; set; } = string.Empty;
        public int TournamentId { get; set; }
    }
}
