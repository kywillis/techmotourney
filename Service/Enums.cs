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
        Completed = 1
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
 }
