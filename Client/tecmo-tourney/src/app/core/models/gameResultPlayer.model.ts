export interface IGameResultPlayer {
    playerName: string;
    teamName: string;
    playerId: number;
    /** Sprite index from `faces.png`; omitted or invalid values fall back to face 1 in the UI. */
    profilePic?: number;
    /** Some API payloads use PascalCase on nested stats. */
    ProfilePic?: number;
    gameTeamId: number | null;
    score: number;
    passingYards: number;
    rushingYards: number;
}