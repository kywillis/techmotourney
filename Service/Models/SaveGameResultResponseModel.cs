namespace TecmoTourney.Models
{
    public class SaveGameResultResponseModel
    {
        public GameResultModel GameResult { get; set; } = null!;
        public OddsGenerationStatusModel OddsGeneration { get; set; } = new();
        public RecalculateBracketResponseModel? BracketReconciliation { get; set; }
    }
}
