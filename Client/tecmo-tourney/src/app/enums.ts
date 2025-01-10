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