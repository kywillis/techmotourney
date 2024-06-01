namespace TecmoTourney.DataAccess.Models
{
    public class GameResultDAOModel
    {
        public int GameResultId { get; set; }
        public int Player1Id { get; set; }
        public int Player2Id { get; set; }
        public int Score1 { get; set; }
        public int Score2 { get; set; }
        public int PassingYards1 { get; set; }
        public int PassingYards2 { get; set; }
        public int RushingYards1 { get; set; }
        public int RushingYards2 { get; set; }
        public int TournamentId { get; set; }
    }
}
