namespace TecmoTourney.Models
{
    /// <summary>Admin summary of house net, pending exposure, and per-player / per-game breakdown for one tournament.</summary>
    public class WagerTournamentSnapshotModel
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; } = string.Empty;
        public decimal SettledHouseNet { get; set; }
        public decimal PendingStakeTotal { get; set; }
        public int PendingWagerCount { get; set; }
        public List<WagerSnapshotPlayerRowModel> Players { get; set; } = new();
        public List<WagerSnapshotGameRowModel> Games { get; set; } = new();
    }

    public class WagerSnapshotPlayerRowModel
    {
        public int PlayerId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public decimal SettledPlayerPnl { get; set; }
        public decimal PendingStake { get; set; }
        public int PendingWagerCount { get; set; }
    }

    public class WagerSnapshotGameRowModel
    {
        public int GameResultId { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal SettledHouseNet { get; set; }
        public decimal PendingStake { get; set; }
        public int PendingWagerCount { get; set; }
    }
}
