import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError } from 'rxjs/operators';
import { Observable, of } from 'rxjs';
import { IGameResult } from '../models/gameResult.model';
import { ISaveGameResultRequest } from '../models/request/saveGameResultRequest.model';
import { ConfigService } from './config.service';
import { IGameSearchParameters } from '../models/gameSearchParameters';
import { IPointSpread } from '../models/pointSpread.model';
import { ISaveGameResultResponse } from '../models/save-game-result-response.model';
import { IPublicWageringSnapshot } from '../models/public-wagering-snapshot.model';

@Injectable({
  providedIn: 'root'
})
export class ResultsService {
  private apiUrl: string;

  constructor(private http: HttpClient, private configService: ConfigService) {
    this.apiUrl = this.configService.getApiUrl() + '/results';
  }

  createResult(request: ISaveGameResultRequest): Observable<ISaveGameResultResponse> {
    return this.http.post<ISaveGameResultResponse>(`${this.apiUrl}`, request);
  }

  getResult(resultId: number): Observable<IGameResult> {
    return this.http.get<IGameResult>(`${this.apiUrl}/${resultId}`);
  }

  getResultsByTournmanentId(tournmanentId: number): Observable<IGameResult[]> {
    return this.http.get<IGameResult[]>(`${this.apiUrl}/tournament/${tournmanentId}`);
  }

  getResultsByPlayertId(playerId: number): Observable<IGameResult[]> {
    return this.http.get<IGameResult[]>(`${this.apiUrl}/player/${playerId}`);
  }

  updateResult(resultId: number, request: ISaveGameResultRequest): Observable<ISaveGameResultResponse> {
    return this.http.put<ISaveGameResultResponse>(`${this.apiUrl}/${resultId}`, request);
  }

  deleteResult(gameResultId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${gameResultId}`);
  }

  searchResulsts(gameSearchParameters: IGameSearchParameters): Observable<IGameResult[]> {
    let url = `${this.apiUrl}/search?`;
    const params: string[] = [];

    if (gameSearchParameters.tournamentId !== null) {
      params.push(`tournamentId=${gameSearchParameters.tournamentId}`);
    }

    if (gameSearchParameters.player1ID !== null) {
      params.push(`player1id=${gameSearchParameters.player1ID}`);
    }

    if (gameSearchParameters.player2ID !== null) {
      params.push(`player2id=${gameSearchParameters.player2ID}`);
    }

    if (gameSearchParameters.matchupLocation !== null) {
      params.push(`matchupLocation=${gameSearchParameters.matchupLocation}`);
    }

    if (params.length > 0) {
      url += params.join('&');
    } else {
      url = `${this.apiUrl}/search`;
    }

    return this.http.get<IGameResult[]>(url);
  }

  createPointSpreads(tournamentId:number, pointSpreads: IPointSpread[]): Observable<IPointSpread[]> {
    return this.http.post<IPointSpread[]>(`${this.apiUrl}/${tournamentId}/pointSpreads`, pointSpreads);
  }
  
  getPointSpreads(tournamentId: number) : Observable<IPointSpread[]> {
    return this.http.get<IPointSpread[]>(`${this.apiUrl}/${tournamentId}/pointSpreads/`);
  }

  /** Public lines + market depth; emits null when the game has no odds (404). */
  getWageringSnapshot(gameResultId: number): Observable<IPublicWageringSnapshot | null> {
    return this.http.get<IPublicWageringSnapshot>(`${this.apiUrl}/games/${gameResultId}/wagering-snapshot`).pipe(
      catchError(() => of(null))
    );
  }

  /** All public snapshots for games in the tournament that have odds; empty array on failure. */
  getWageringSnapshotsByTournament(tournamentId: number): Observable<IPublicWageringSnapshot[]> {
    return this.http
      .get<IPublicWageringSnapshot[]>(`${this.apiUrl}/tournament/${tournamentId}/wagering-snapshots`)
      .pipe(catchError(() => of([])));
  }
}
