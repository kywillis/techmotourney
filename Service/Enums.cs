namespace TecmoTourney
{
    public enum TournamentStatus
    {
        Waiting = 0,
        Preliminaries = 1,
        Tournament = 2,
        Completed = 3,
        Deleted = 4,
    }
    public enum GameStatus
    {
        Waiting = 0,
        Completed = 1,
        InProgress = 2,
    }
    public enum GameType
    {
        Preliminary = 0,
        Tournament = 1
    }

    public enum GameStat
    {
        PassingYards = 0,
        RushingYards = 1,
        TotalYards = 2,
        PassingYardsAllowed = 3,
        RushingYardsAllowed = 4,
        TotalYardsAllowed = 5,
        PointsScoreFor = 6,
        PointsScoreAgainst = 7,
        Wins = 8,
        Losses = 9
    }

    public enum PrelimTieBreaker
    {
        PointsScored = 0,
        PointsAllowed = 2,
        PassingYards = 3,
        RushingYards = 4,
        PassingYardsAllowed = 5,
        RushingYardsAllowed = 6,
        CoinFlip = 7
    }

    public enum TournamentBracketUpdateStatus
    {
        New = 1,
        Complete = 2
    }

    public enum BracketLocation
    {
        Winners = 1,
        Losers = 2,
        Championship = 3,
        /// <summary>Preliminary-round games (TC_GameOdds.BracketTypeId for prelim matchups).</summary>
        Preliminary = 4,
    }

    public enum PointSpreadStatus
    {
        Waiting = 1,
        Complete = 2
    }

    public enum PendingActivationStatus
    {
        Pending = 0,
        Activated = 1
    }

    public enum WagerStatus
    {
        Pending = 0,
        Won = 1,
        Lost = 2,
        Void = 3,
        Cancelled = 4
    }

    public enum WagerMarketType
    {
        Spread = 0,
        OverUnder = 1,
        MoneyLine = 2
    }

    public enum WagerSide
    {
        Player1Spread = 0,
        Player2Spread = 1,
        Over = 2,
        Under = 3,
        Player1ML = 4,
        Player2ML = 5
    }

    public enum WagerAuditAction
    {
        PlaceWager = 0,
        SettleWagerWin = 1,
        SettleWagerLose = 2,
        VoidWager = 3,
        CancelWager = 4,
        BalanceSet = 5,
        BalanceAdd = 6,
        BalanceSetToZero = 7,
        /// <summary>Admin refunded a pending wager (any game state).</summary>
        AdminCancelWager = 8,

        /// <summary>Game or odds row removed; pending wagers cancelled/refunded; settled wagers detached from game.</summary>
        GameResultRemoved = 9,

        /// <summary>Undo prior settlement balance effect before re-grading when game result changes.</summary>
        ReverseSettlement = 10
    }

    public enum WagerBalanceAction
    {
        Set = 0,
        Add = 1,
        SetToZero = 2
    }
}
