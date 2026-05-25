using TecmoTourney;

namespace TecmoTourney.DataAccess.Models
{
    public class WagerDAOModel
    {
        public int WagerId { get; set; }
        public int PlayerId { get; set; }
        public int GameResultId { get; set; }
        public int TournamentId { get; set; }
        public WagerMarketType MarketType { get; set; }
        public WagerSide Side { get; set; }
        public decimal StakeAmount { get; set; }
        public WagerStatus Status { get; set; } = WagerStatus.Pending;
        public DateTime CreatedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? SettledAt { get; set; }
    }
}
