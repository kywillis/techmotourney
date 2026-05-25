namespace TecmoTourney.DataAccess.Models
{
    public class GameResultDAOModel
    {
        public int GameResultId { get; set; }
        public int Player1Id { get; set; }
        public int Player2Id { get; set; }
        public int Player1Score { get; set; }
        public int Player2Score { get; set; }
        public int Player1PassingYards { get; set; }
        public int Player2PassingYards { get; set; }
        public int Player1RushingYards { get; set; }
        public int Player2RushingYards { get; set; }
        public int TournamentId { get; set; }
        public int? Player1GameTeamID { get; set; }
        public int? Player2GameTeamID { get; set; }
        public int StatusId { get; set; }
        public int GameTypeId { get; set; }
        public bool IsDeleted { get; set; }
        public int? BracketGameId { get; set; }
        /// <summary>
        /// If set, this game does not count toward this player's preliminary seeding/standings (e.g. their 3rd game when odd N).
        /// </summary>
        public int? SeedingExemptPlayerId { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime? GameStartedAt { get; set; }
    }
}
