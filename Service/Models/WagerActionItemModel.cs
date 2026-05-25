using TecmoTourney;

namespace TecmoTourney.Models
{
    /// <summary>One wager on a game (for "action" display when ShowActionOnGames is true).</summary>
    public class WagerActionItemModel
    {
        public string PlayerName { get; set; } = string.Empty;
        public WagerSide Side { get; set; }
        public decimal StakeAmount { get; set; }
    }
}
