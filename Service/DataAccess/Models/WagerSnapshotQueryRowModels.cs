namespace TecmoTourney.DataAccess.Models
{
    public class PlayerTournamentPnlRowDAOModel
    {
        public int PlayerId { get; set; }
        public decimal SettledPnl { get; set; }
    }

    public class PendingStakeByPlayerRowDAOModel
    {
        public int PlayerId { get; set; }
        public decimal StakeTotal { get; set; }
        public int WagerCount { get; set; }
    }

    public class PendingStakeByGameRowDAOModel
    {
        public int GameResultId { get; set; }
        public decimal StakeTotal { get; set; }
        public int WagerCount { get; set; }
    }
}
