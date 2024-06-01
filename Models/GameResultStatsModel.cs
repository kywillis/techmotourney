using System;

namespace TecmoTourney.Models
{
    public class GameResultStatsModel
    {
        public int PlayerId { get; set; }
        public int TeamId { get; set; }
        public int Score { get; set; }
        public int PassingYards { get; set; }
        public int RushingYards { get; set; }
    }
}
