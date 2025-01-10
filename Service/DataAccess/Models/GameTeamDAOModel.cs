namespace TecmoTourney.DataAccess.Models
{
    public class GameTeamDAOModel
    {
        public int GameTeamId { get; set; }
        public string TeamLocation { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public int DefenseRanking { get; set; }
        public int OffenseRanking { get; set; }
    }
}
