using TecmoTourney;

namespace TecmoTourney.DataAccess.Models
{
    public class WagerAuditDAOModel
    {
        public int AuditId { get; set; }
        public int? TournamentId { get; set; }
        public int TargetPlayerId { get; set; }
        public int? ActorPlayerId { get; set; }
        public WagerAuditAction Action { get; set; }
        public int? WagerId { get; set; }
        public int? GameResultId { get; set; }
        public decimal? Amount { get; set; }
        public decimal? BalanceBefore { get; set; }
        public decimal? BalanceAfter { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
