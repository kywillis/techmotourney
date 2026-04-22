namespace TecmoTourney.Models
{
    public class ChangeTournamentStatusResponseModel
    {
        public TournamentModel Tournament { get; set; } = null!;
        public OddsGenerationStatusModel OddsGeneration { get; set; } = new();
    }
}
