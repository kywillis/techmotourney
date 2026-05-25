import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ConfigService } from './config.service';
import { WagerApiService } from './wager-api.service';
import { MyWager } from '../models/my-wager.model';
import { firstValueFrom } from 'rxjs';
import { BettableGame } from '../models/bettable-game.model';
import { AdminPlayerLinkListItem, PendingActivation } from '../models/pending-activation.model';

export interface AdminUpdateGameOddsRequest {
  spread: number;
  favoredPlayerId: number | null;
  moneyLinePlayer1: number | null;
  moneyLinePlayer2: number | null;
  overUnder: number | null;
}

export interface SaveGameResultRequest {
  gameResultId?: number | null;
  player1: {
    playerId: number;
    gameTeamId?: number | null;
    bracketGameId?: number | null;
    score: number;
    passingYards: number;
    rushingYards: number;
  };
  player2: {
    playerId: number;
    gameTeamId?: number | null;
    bracketGameId?: number | null;
    score: number;
    passingYards: number;
    rushingYards: number;
  };
  tournamentId: number;
  status: string;
  gameType: string;
  bracketGameId: number;
}

export interface AdminGameResultRow {
  gameResultId: number;
  tournamentId: number;
  player1: {
    playerId: number;
    playerName?: string;
    gameTeamId?: number | null;
    score: number;
    passingYards: number;
    rushingYards: number;
  };
  player2: {
    playerId: number;
    playerName?: string;
    gameTeamId?: number | null;
    score: number;
    passingYards: number;
    rushingYards: number;
  };
  status: string;
  gameType: string;
  bracketGameId: number;
  matchUpIndex?: number;
  date?: string;
}

export interface WagerBalanceAdminRequest {
  playerId: number;
  action: 'Set' | 'Add' | 'SetToZero';
  amount?: number | null;
}

export interface AdminPlayerBalanceListItem {
  playerId: number;
  fullName: string;
  balance: number;
}

/** TC_GameTeams row for admin dropdowns (value = id, label = teamName). */
export interface GameTeamOption {
  gameTeamId: number;
  teamName: string;
  teamLocation: string;
}

@Injectable({
  providedIn: 'root'
})
export class WagerAdminApiService {
  private http = inject(HttpClient);
  private config = inject(ConfigService);
  private wagerApi = inject(WagerApiService);

  private get adminBase(): string {
    return `${this.config.getApiUrl()}/wager/admin`;
  }

  getPendingActivations(includeActivated = false): Promise<PendingActivation[]> {
    const pick = (raw: Record<string, unknown>, c: string, p: string) => raw[c] ?? raw[p];
    const str = (v: unknown) => (v == null ? '' : String(v));
    const num = (v: unknown) => (typeof v === 'number' ? v : Number(v));
    return firstValueFrom(
      this.http.get<Record<string, unknown>[]>(`${this.adminBase}/pending-activations`, {
        params: { includeActivated: String(includeActivated) }
      })
    ).then((rows) =>
      rows.map((raw) => ({
        pendingActivationId: num(pick(raw, 'pendingActivationId', 'PendingActivationId')),
        googleSubjectId: str(pick(raw, 'googleSubjectId', 'GoogleSubjectId')),
        email: str(pick(raw, 'email', 'Email')),
        fullName: str(pick(raw, 'fullName', 'FullName')),
        requestedProfilePic: num(pick(raw, 'requestedProfilePic', 'RequestedProfilePic')),
        status: str(pick(raw, 'status', 'Status')),
        requestedAt: str(pick(raw, 'requestedAt', 'RequestedAt')),
        activatedAt:
          pick(raw, 'activatedAt', 'ActivatedAt') == null || pick(raw, 'activatedAt', 'ActivatedAt') === ''
            ? null
            : str(pick(raw, 'activatedAt', 'ActivatedAt')),
        activatedByPlayerId:
          pick(raw, 'activatedByPlayerId', 'ActivatedByPlayerId') == null ||
          pick(raw, 'activatedByPlayerId', 'ActivatedByPlayerId') === ''
            ? null
            : num(pick(raw, 'activatedByPlayerId', 'ActivatedByPlayerId'))
      }))
    );
  }

  getPlayersEligibleForGoogleLink(): Promise<AdminPlayerLinkListItem[]> {
    const str = (v: unknown) => (v == null ? '' : String(v));
    const num = (v: unknown) => (typeof v === 'number' ? v : Number(v));
    const pick = (raw: Record<string, unknown>, c: string, p: string) => raw[c] ?? raw[p];
    return firstValueFrom(
      this.http.get<Record<string, unknown>[]>(`${this.adminBase}/players/eligible-google-link`)
    ).then((rows) =>
      rows.map((raw) => ({
        playerId: num(pick(raw, 'playerId', 'PlayerId')),
        fullName: str(pick(raw, 'fullName', 'FullName')).trim() || `Player ${num(pick(raw, 'playerId', 'PlayerId'))}`,
        emailAddress: str(pick(raw, 'emailAddress', 'EmailAddress')).trim()
      }))
    );
  }

  linkPendingToPlayer(pendingActivationId: number, playerId: number): Promise<void> {
    return firstValueFrom(
      this.http.post(`${this.adminBase}/pending-activations/${pendingActivationId}/link-to-player`, {
        playerId
      })
    ).then(() => undefined);
  }

  /** Creates a new TC_Players row from a pending signup (admin). */
  activatePendingCreateNew(
    pendingActivationId: number,
    body: { fullName: string; emailAddress: string; profilePic: number }
  ): Promise<void> {
    return firstValueFrom(
      this.http.post(`${this.adminBase}/pending-activations/${pendingActivationId}/activate`, body)
    ).then(() => undefined);
  }

  private get resultsBase(): string {
    return `${this.config.getApiUrl()}/results`;
  }

  /** All Tecmo teams from TC_GameTeams (public api/gameTeams). */
  getGameTeams(): Promise<GameTeamOption[]> {
    const str = (v: unknown) => (v == null ? '' : String(v));
    const num = (v: unknown) => (typeof v === 'number' ? v : Number(v));
    const pick = (raw: Record<string, unknown>, c: string, p: string) => raw[c] ?? raw[p];
    return firstValueFrom(
      this.http.get<Record<string, unknown>[]>(`${this.config.getApiUrl()}/gameTeams`)
    ).then((rows) =>
      rows
        .map((raw) => ({
          gameTeamId: num(pick(raw, 'gameTeamId', 'GameTeamId')),
          teamName: str(pick(raw, 'teamName', 'TeamName')).trim() || `Team ${num(pick(raw, 'gameTeamId', 'GameTeamId'))}`,
          teamLocation: str(pick(raw, 'teamLocation', 'TeamLocation')).trim()
        }))
        .filter((t) => t.gameTeamId > 0)
        .sort((a, b) => a.teamName.localeCompare(b.teamName, undefined, { sensitivity: 'base' }))
    );
  }

  getPendingWagers(tournamentId: number): Promise<MyWager[]> {
    return firstValueFrom(
      this.http.get<Record<string, unknown>[]>(`${this.adminBase}/pending-wagers`, {
        params: { tournamentId: tournamentId.toString() }
      })
    ).then((rows) => rows.map((r) => this.wagerApi.normalizeMyWager(r)));
  }

  adminCancelWager(wagerId: number): Promise<boolean> {
    return firstValueFrom(this.http.post<boolean>(`${this.adminBase}/wagers/${wagerId}/cancel`, {}));
  }

  updateGameOdds(gameResultId: number, body: AdminUpdateGameOddsRequest): Promise<boolean> {
    return firstValueFrom(this.http.put<boolean>(`${this.adminBase}/games/${gameResultId}/odds`, body));
  }

  saveGameResult(body: SaveGameResultRequest): Promise<AdminGameResultRow> {
    return firstValueFrom(this.http.post<AdminGameResultRow>(`${this.adminBase}/game-result`, body));
  }

  updatePlayerBalance(body: WagerBalanceAdminRequest): Promise<boolean> {
    return firstValueFrom(this.http.patch<boolean>(`${this.adminBase}/balance`, body));
  }

  getPlayersForBalanceAdmin(tournamentId: number): Promise<AdminPlayerBalanceListItem[]> {
    const str = (v: unknown) => (v == null ? '' : String(v));
    const num = (v: unknown) => (typeof v === 'number' ? v : Number(v));
    const pick = (raw: Record<string, unknown>, c: string, p: string) => raw[c] ?? raw[p];
    return firstValueFrom(
      this.http.get<Record<string, unknown>[]>(`${this.adminBase}/players`, {
        params: { tournamentId: tournamentId.toString() }
      })
    ).then((rows) =>
      rows.map((raw) => ({
        playerId: num(pick(raw, 'playerId', 'PlayerId')),
        fullName: str(pick(raw, 'fullName', 'FullName')).trim() || `Player ${num(pick(raw, 'playerId', 'PlayerId'))}`,
        balance: num(pick(raw, 'balance', 'Balance'))
      }))
    );
  }

  getTournamentResults(tournamentId: number): Promise<AdminGameResultRow[]> {
    return firstValueFrom(
      this.http.get<Record<string, unknown>[]>(`${this.resultsBase}/tournament/${tournamentId}`)
    ).then((rows) => rows.map((r) => this.normalizeGameResultRow(r)));
  }

  private normalizeGameResultRow(raw: Record<string, unknown>): AdminGameResultRow {
    const pick = (c: string, p: string) => raw[c] ?? raw[p];
    const num = (v: unknown) => (typeof v === 'number' ? v : Number(v));
    const str = (v: unknown) => (v == null ? '' : String(v));
    const stats = (camel: string, pascal: string) => {
      const o = (pick(camel, pascal) ?? {}) as Record<string, unknown>;
      const pk = (c: string, p: string) => o[c] ?? o[p];
      return {
        playerId: num(pk('playerId', 'PlayerId')),
        playerName: str(pk('playerName', 'PlayerName')) || undefined,
        gameTeamId:
          pk('gameTeamId', 'GameTeamId') == null || pk('gameTeamId', 'GameTeamId') === ''
            ? null
            : num(pk('gameTeamId', 'GameTeamId')),
        score: num(pk('score', 'Score')),
        passingYards: num(pk('passingYards', 'PassingYards')),
        rushingYards: num(pk('rushingYards', 'RushingYards'))
      };
    };
    return {
      gameResultId: num(pick('gameResultId', 'GameResultId')),
      tournamentId: num(pick('tournamentId', 'TournamentId')),
      player1: stats('player1', 'Player1'),
      player2: stats('player2', 'Player2'),
      status: str(pick('status', 'Status')),
      gameType: str(pick('gameType', 'GameType')),
      bracketGameId: num(pick('bracketGameId', 'BracketGameId')),
      matchUpIndex:
        pick('matchUpIndex', 'MatchUpIndex') == null ? undefined : num(pick('matchUpIndex', 'MatchUpIndex')),
      date: pick('date', 'Date') == null ? undefined : str(pick('date', 'Date'))
    };
  }

  /** Odds + names for admin editor (any game state; not the public bettor-only endpoint). */
  getGameLinesForAdmin(gameResultId: number): Promise<BettableGame> {
    return firstValueFrom(this.http.get<BettableGame>(`${this.adminBase}/games/${gameResultId}/lines`));
  }
}
