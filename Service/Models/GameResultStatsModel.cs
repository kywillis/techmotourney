using System;
using System.Text.Json.Serialization;

namespace TecmoTourney.Models
{
    public class GameResultStatsModel
    {
        public int PlayerId { get; set; }

        /// <summary>Sprite index for faces.png; always serialized so clients (e.g. game-station) never miss the field.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public int ProfilePic { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int? GameTeamId { get; set; }
        public int? BracketGameId { get; set; }
        public string TeamName { get; set; } = string.Empty ;
        public int Score { get; set; }
        public int PassingYards { get; set; }
        public int RushingYards { get; set; }
    }
}
