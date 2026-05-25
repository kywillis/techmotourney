import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ConfigService } from './config.service';
import { BettableGame } from '../models/bettable-game.model';
import { WagerGamesBoard } from '../models/wager-games-board.model';
import { Tournament } from '../models/tournament.model';
import { PlaceWagerRequest } from '../models/place-wager-request.model';
import { MyWager } from '../models/my-wager.model';
import { WagerAuditEntry } from '../models/wager-audit-entry.model';
import { TournamentSummary } from '../models/tournament-summary.model';
import { firstValueFrom } from 'rxjs';
import { catchError, of } from 'rxjs';

export interface WagerModel {
  wagerId: number;
  gameResultId: number;
  playerId: number;
  tournamentId: number;
  marketType: string;
  side: string;
  stakeAmount: number;
  status: string;
  createdAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class WagerApiService {
  constructor(
    private http: HttpClient,
    private config: ConfigService
  ) {}

  private get base(): string {
    return `${this.config.getApiUrl()}/wager`;
  }

  getGames(): Promise<BettableGame[]> {
    return firstValueFrom(this.http.get<BettableGame[]>(`${this.base}/games`));
  }

  getGamesBoard(): Promise<WagerGamesBoard> {
    return firstValueFrom(this.http.get<WagerGamesBoard>(`${this.base}/wager-games-board`));
  }

  getGameDetail(gameResultId: number): Promise<BettableGame> {
    return firstValueFrom(this.http.get<BettableGame>(`${this.base}/games/${gameResultId}`));
  }

  getActiveTournament(): Promise<Tournament | null> {
    return firstValueFrom(
      this.http.get<Tournament>(`${this.base}/tournament/active`).pipe(
        catchError(() => of(null))
      )
    );
  }

  getTournaments(): Promise<Tournament[]> {
    return firstValueFrom(this.http.get<Tournament[]>(`${this.base}/tournament`));
  }

  placeWager(request: PlaceWagerRequest): Promise<WagerModel> {
    return firstValueFrom(this.http.post<WagerModel>(`${this.base}/wagers`, request));
  }

  getMyAudit(tournamentId: number): Promise<WagerAuditEntry[]> {
    return firstValueFrom(
      this.http.get<Record<string, unknown>[]>(`${this.base}/audit`, {
        params: { tournamentId: tournamentId.toString() }
      })
    ).then(rows => rows.map(r => this.normalizeAuditEntry(r)));
  }

  getTournamentSummary(tournamentId: number): Promise<TournamentSummary | null> {
    return firstValueFrom(
      this.http
        .get<TournamentSummary>(`${this.base}/tournament/${tournamentId}/summary`)
        .pipe(catchError(() => of(null)))
    );
  }

  /** Pass active tournament id so results are scoped to that event. */
  getMyWagers(tournamentId: number): Promise<MyWager[]> {
    return firstValueFrom(
      this.http.get<Record<string, unknown>[]>(`${this.base}/wagers`, {
        params: { tournamentId: tournamentId.toString() }
      })
    ).then(rows => rows.map(r => this.normalizeMyWager(r)));
  }

  normalizeMyWager(raw: Record<string, unknown>): MyWager {
    const str = (v: unknown) => (v == null ? '' : String(v));
    const num = (v: unknown) => (typeof v === 'number' ? v : Number(v));
    const pick = (camel: string, pascal: string) => raw[camel] ?? raw[pascal];
    return {
      wagerId: num(pick('wagerId', 'WagerId')),
      playerId: num(pick('playerId', 'PlayerId')),
      gameResultId: num(pick('gameResultId', 'GameResultId')),
      tournamentId: num(pick('tournamentId', 'TournamentId')),
      marketType: str(pick('marketType', 'MarketType')),
      side: str(pick('side', 'Side')),
      stakeAmount: num(pick('stakeAmount', 'StakeAmount')),
      status: str(pick('status', 'Status')) as MyWager['status'],
      createdAt: str(pick('createdAt', 'CreatedAt')),
      cancelledAt:
        pick('cancelledAt', 'CancelledAt') == null
          ? null
          : str(pick('cancelledAt', 'CancelledAt')),
      settledAt:
        pick('settledAt', 'SettledAt') == null
          ? null
          : str(pick('settledAt', 'SettledAt')),
      player1Name: str(pick('player1Name', 'Player1Name')),
      player2Name: str(pick('player2Name', 'Player2Name')),
      pickDescription: str(pick('pickDescription', 'PickDescription')),
      potentialPayout: num(pick('potentialPayout', 'PotentialPayout')),
      bettorFullName: (() => {
        const b = pick('bettorFullName', 'BettorFullName');
        return b == null || b === '' ? undefined : str(b);
      })()
    };
  }

  private normalizeAuditEntry(raw: Record<string, unknown>): WagerAuditEntry {
    const str = (v: unknown) => (v == null ? '' : String(v));
    const num = (v: unknown) => (typeof v === 'number' ? v : Number(v));
    const pick = (camel: string, pascal: string) => raw[camel] ?? raw[pascal];
    const nullableNum = (v: unknown): number | null => {
      if (v == null || v === '') return null;
      const n = typeof v === 'number' ? v : Number(v);
      return Number.isNaN(n) ? null : n;
    };
    return {
      auditId: num(pick('auditId', 'AuditId')),
      tournamentId: nullableNum(pick('tournamentId', 'TournamentId')),
      targetPlayerId: num(pick('targetPlayerId', 'TargetPlayerId')),
      actorPlayerId: nullableNum(pick('actorPlayerId', 'ActorPlayerId')),
      action: str(pick('action', 'Action')),
      wagerId: nullableNum(pick('wagerId', 'WagerId')),
      gameResultId: nullableNum(pick('gameResultId', 'GameResultId')),
      amount: nullableNum(pick('amount', 'Amount')),
      balanceBefore: nullableNum(pick('balanceBefore', 'BalanceBefore')),
      balanceAfter: nullableNum(pick('balanceAfter', 'BalanceAfter')),
      createdAt: str(pick('createdAt', 'CreatedAt'))
    };
  }
}
