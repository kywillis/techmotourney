using System;

namespace TecmoTourney.Models
{
    public class GameResultStatsModel
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int? GameTeamId { get; set; }
        public int? BracketGameId { get; set; }
        public string GameTeam { get; set; } = string.Empty ;
        public int Score { get; set; }
        public int PassingYards { get; set; }
        public int RushingYards { get; set; }
    }
}
