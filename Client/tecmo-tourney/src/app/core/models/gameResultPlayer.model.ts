export interface IGameResultPlayer {
    playerName: string;
    teamName: string;
    playerId: number;
    gameTeamId: number | null;
    score: number;
    passingYards: number;
    rushingYards: number;
}