namespace TecmoTourney.Models
{
    public class PlayerSummaryModel : PlayerModel
    {
        public int Wins { get; set; }
        public int Loses { get; set; }
        public ICollection<int> TournamentIds { get; set; } = new List<int>();
    }
}
