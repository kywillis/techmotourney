import { PrelimTieBreaker } from "src/app/enums";

export interface ITournamentStanding {
    tournamentId: number;
    gamesPlayed: number;
    playerId: number;
    playerName: string;
    preliminariesScore: number;
    preliminaryPosition: number;
    preliminariesTieBreakerUsed?: PrelimTieBreaker | null;
    tournamentFinishPosition: number;
    totalPointsFor: number;
    totalPointsAgainst: number;
    totalPassingYards: number;
    totalRushingYards: number;
    totalPassingYardsAllowed: number;
    totalRushingYardsAllowed: number;
    wins: number;
    loses: number;
  }