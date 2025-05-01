import { IMatchUpResult } from "./matchupResult.model";

export interface ITeamHistoryResult extends IMatchUpResult{
    teamName: string;
    teamId: number;
}