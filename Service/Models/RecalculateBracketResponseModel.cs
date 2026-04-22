namespace TecmoTourney.Models;

public class RecalculateBracketResponseModel
{
    public List<int> CreatedGameResultIds { get; set; } = new();
    public List<int> SoftDeletedGameResultIds { get; set; } = new();
    public OddsGenerationStatusModel OddsGeneration { get; set; } = new();
    public bool Skipped { get; set; }
    public string? SkipReason { get; set; }
}
