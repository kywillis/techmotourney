using System.Text.Json.Serialization;
using Newtonsoft.Json;
namespace TecmoTourney.Models
{
    public class TournamentBracketModel
    {
        [JsonProperty("teams")]
        public List<List<BracketTeam?>> Teams { get; set; } = new List<List<BracketTeam?>>();
        [JsonProperty("results")]
        public List<List<List<List<object>>>> Results { get; set; } = new()
        {
            new List<List<List<object>>>
            {
                new List<List<object>>
                {
                    new List<object>()
                }
            },
            new List<List<List<object>>>(),
            new List<List<List<object>>>()
        };

        /// <summary>
        /// creates the bracket structure for the tournament
        /// </summary>
        /// <param name="size"></param>
        public void PopulateBracket(int size)
        { 
            switch(size)
            {
                case 4:
                    Teams = build4Teams();
                    break;
                case 5:
                    Teams = build5Teams();
                    break;
                case 6:
                    Teams = build6Teams();
                    break;
                case 7:
                    Teams = build7Teams();
                    break;
                case 8:
                    Teams = build8Teams();
                    break;
                case 9:
                    Teams = build9Teams();
                    break;
                case 10:
                    Teams = build10Teams();
                    break;
                case 11:
                    Teams = build11Teams();
                    break;
                case 12:
                    Teams = build12Teams();
                    break;
                case 13:
                    Teams = build13Teams();
                    break;
                case 14:
                    Teams = build14Teams();
                    break;
                case 15:
                    Teams = build15Teams();
                    break;
                case 16:
                    Teams = build16Teams();
                    break;
            }
        }

        private List<List<BracketTeam?>> build4Teams()
        {
            return new List<List<BracketTeam?>>
            {
                new List<BracketTeam?> { new BracketTeam { Seed = 1 }, new BracketTeam { Seed = 4 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 2 }, new BracketTeam { Seed = 3 } },
            };
        }

        private List<List<BracketTeam?>> build5Teams()
        {
            return new List<List<BracketTeam?>>
            {
                new List<BracketTeam?> { new BracketTeam { Seed = 1 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 4 }, new BracketTeam { Seed = 5 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 3 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 2 }, null },
            };
        }

        private List<List<BracketTeam?>> build6Teams()
        {
            return new List<List<BracketTeam?>>
            {
                new List<BracketTeam?> { new BracketTeam { Seed = 1 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 5 }, new BracketTeam { Seed = 6 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 3 }, new BracketTeam { Seed = 4 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 2 }, null },
            };
        }

        private List<List<BracketTeam?>> build7Teams()
        {
            return new List<List<BracketTeam?>>
            {
                new List<BracketTeam?> { new BracketTeam { Seed = 1 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 6 }, new BracketTeam { Seed = 7 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 3 }, new BracketTeam { Seed = 4 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 2 }, new BracketTeam { Seed = 5 } },
            };
        }
        private List<List<BracketTeam?>> build8Teams()
        {
            return new List<List<BracketTeam?>>
            {
                new List<BracketTeam?> { new BracketTeam { Seed = 1 }, new BracketTeam { Seed = 8 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 6 }, new BracketTeam { Seed = 7 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 3 }, new BracketTeam { Seed = 4 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 2 }, new BracketTeam { Seed = 5 } },
            };
        }
        private List<List<BracketTeam?>> build9Teams()
        {
            return new List<List<BracketTeam?>>
            {
                new List<BracketTeam?> { new BracketTeam { Seed = 1 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 8 }, new BracketTeam { Seed = 9 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 5 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 4 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 3 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 6 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 7 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 2 }, null },
            };
        }
        private List<List<BracketTeam?>> build10Teams()
        {
            return new List<List<BracketTeam?>>
            {
                new List<BracketTeam?> { new BracketTeam { Seed = 1 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 8 }, new BracketTeam { Seed = 9 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 5 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 4 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 3 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 6 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 7 }, new BracketTeam { Seed = 10 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 2 }, null },
            };
        }
        private List<List<BracketTeam?>> build11Teams()
        {
            return new List<List<BracketTeam?>>
            {
                new List<BracketTeam?> { new BracketTeam { Seed = 1 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 8 }, new BracketTeam { Seed = 9 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 5 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 4 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 3 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 6 }, new BracketTeam { Seed = 11 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 7 }, new BracketTeam { Seed = 10 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 2 }, null },
            };
        }
        private List<List<BracketTeam?>> build12Teams()
        {
            return new List<List<BracketTeam?>>
            {
                new List<BracketTeam?> { new BracketTeam { Seed = 1 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 8 }, new BracketTeam { Seed = 9 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 5 }, new BracketTeam { Seed = 12 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 4 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 3 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 6 }, new BracketTeam { Seed = 11 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 7 }, new BracketTeam { Seed = 10 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 2 }, null },
            };
        }
        private List<List<BracketTeam?>> build13Teams()
        {
            return new List<List<BracketTeam?>>
            {
                new List<BracketTeam?> { new BracketTeam { Seed = 1 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 8 }, new BracketTeam { Seed = 9 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 5 }, new BracketTeam { Seed = 12 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 4 }, new BracketTeam { Seed = 13 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 3 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 6 }, new BracketTeam { Seed = 11 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 7 }, new BracketTeam { Seed = 10 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 2 }, null },
            };
        }
        private List<List<BracketTeam?>> build14Teams()
        {
            return new List<List<BracketTeam?>>
            {
                new List<BracketTeam?> { new BracketTeam { Seed = 1 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 8 }, new BracketTeam { Seed = 9 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 5 }, new BracketTeam { Seed = 12 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 4 }, new BracketTeam { Seed = 13 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 3 }, new BracketTeam { Seed = 14 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 6 }, new BracketTeam { Seed = 11 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 7 }, new BracketTeam { Seed = 10 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 2 }, null },
            };
        }
        private List<List<BracketTeam?>> build15Teams()
        {
            return new List<List<BracketTeam?>>
            {
                new List<BracketTeam?> { new BracketTeam { Seed = 1 }, null },
                new List<BracketTeam?> { new BracketTeam { Seed = 8 }, new BracketTeam { Seed = 9 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 5 }, new BracketTeam { Seed = 12 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 4 }, new BracketTeam { Seed = 13 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 3 }, new BracketTeam { Seed = 14 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 6 }, new BracketTeam { Seed = 11 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 7 }, new BracketTeam { Seed = 10 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 2 }, new BracketTeam { Seed = 15 } },
            };
        }
        private List<List<BracketTeam?>> build16Teams()
        {
            return new List<List<BracketTeam?>>
            {
                new List<BracketTeam?> { new BracketTeam { Seed = 1 }, new BracketTeam { Seed = 16 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 8 }, new BracketTeam { Seed = 9 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 5 }, new BracketTeam { Seed = 12 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 4 }, new BracketTeam { Seed = 13 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 3 }, new BracketTeam { Seed = 14 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 6 }, new BracketTeam { Seed = 11 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 7 }, new BracketTeam { Seed = 10 } },
                new List<BracketTeam?> { new BracketTeam { Seed = 2 }, new BracketTeam { Seed = 15 } },
            };
        }
    }

    public class BracketTeam
    {
        [JsonProperty("seed")]
        public int Seed { get; set; }
        [JsonProperty("player")]
        public string Player { get; set; } = "";
        [JsonProperty("playerId")]
        public int PlayerId { get; set; }
        [JsonProperty("gameId")]
        public int GameId { get; set; }
    }
     
    
}
