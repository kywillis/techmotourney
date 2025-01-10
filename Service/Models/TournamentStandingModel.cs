namespace TecmoTourney.Models
{
    public class TournamentStandingModel
    {
        public int TournamentId { get; set; }
        public int GamesPlayed { get; set; }
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int PreliminariesScore { get; set; }
        public int PreliminaryPosition { get; set; }
        public PrelimTieBreaker? PreliminariesTieBreakerUsed { get; set; }
        public int TournamentFinishPosition { get; set; }
        public int TotalPointsFor { get; set; }
        public int TotalPointsAgainst { get; set; }
        public int TotalPassingYards { get; set; }
        public int TotalRushingYards { get; set; }
        public int TotalPassingYardsAllowed { get; set; }
        public int TotalRushingYardsAllowed { get; set; }
        public int Wins { get; set; }
        public int Loses { get; set; }
        public int Seed { get; set; }
    }
}
