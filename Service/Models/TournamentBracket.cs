using System.Text.Json.Serialization;
using Newtonsoft.Json;
namespace TecmoTourney.Models
{
    public class TournamentBracketModel
    {
        public List<List<BracketTeam?>> Teams { get; set; } = new List<List<BracketTeam?>>();
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
    }

    public class BracketTeam
    {
        public int Seed { get; set; }
        public string Player { get; set; } = "";
        public int PlayerId { get; set; }
        public int GameId { get; set; }
    }
     
    
}
