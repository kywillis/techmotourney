namespace TecmoTourney.Models
{
    /// <summary>Active tournament games grouped for the wager games list UI.</summary>
    public class WagerGamesBoardModel
    {
        public List<BettableGameModel> OpenForBetting { get; set; } = new();
        public List<BettableGameModel> InProgress { get; set; } = new();
        public List<BettableGameModel> Completed { get; set; } = new();
    }
}
