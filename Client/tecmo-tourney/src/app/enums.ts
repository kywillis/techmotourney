// enums.ts
export enum TournamentStatus {
    Waiting = "Waiting",
    Preliminaries = "Preliminaries",
    Tournament = "Tournament",
    Completed = "Completed",
    Deleted = "Deleted",
}

export enum GameStatus {
    Waiting = "Waiting",
    Completed = "Completed",
}

export enum GameType {
    Preliminary = "Preliminary",
    Tournament = "Tournament",
}

export enum PrelimTieBreaker {
    PointsScored = "PointsScored",
    PointsAllowed = "PointsAllowed",
    PassingYards = "PassingYards",
    RushingYards = "RushingYards",
    PassingYardsAllowed = "PassingYardsAllowed",
    RushingYardsAllowed = "RushingYardsAllowed",
    CoinFlip = "CoinFlip"
  }

  export enum StatType {
    Games = 'Games',
    HighestScore = 'Highest Score',
    TotalOffensiveYards = 'Total Offensive Yards',
    TopPassingYards = 'Top Passing Yards',
    TopRushingYards = 'Top Rushing Yards',
    FewestPointsAllowed = 'Fewest Points Allowed'
  }

  export enum BracketLocation {
    Winners = 'winners',
    Losers = 'losers',
    Champinship = 'champinship'
  }

  export enum PlayerStatDetailsType{
    TeamPlayedWith = 1,
    TeamPlayedAgainst = 2,
    Opponent = 3,
  }