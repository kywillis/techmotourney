import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { interval, Observable, switchMap } from 'rxjs';
import { IGameResult } from '../models/gameResult.model';
import { ISaveGameResultRequest } from '../models/request/saveGameResultRequest.model';
import { ConfigService } from './config.service';
import { ITournamentBracketUpdate } from '../models/tournamentBracketUpdate.model';
import { IGameSearchParameters } from '../models/gameSearchParameters';
import { IPointSpread } from '../models/pointSpread.model';

@Injectable({
  providedIn: 'root'
})
export class ResultsService {
  private apiUrl: string;

  constructor(private http: HttpClient, private configService: ConfigService) {
    this.apiUrl = this.configService.getApiUrl() + '/results';
  }

  createResult(request: ISaveGameResultRequest): Observable<IGameResult> {
    return this.http.post<IGameResult>(`${this.apiUrl}`, request);
  }

  getBracketUpdates(tournamentId: number): Observable<ITournamentBracketUpdate[]> {
    return interval(10 * 1000).pipe(
      switchMap(() => this.http.get<ITournamentBracketUpdate[]>(`${this.apiUrl}/gameUpdates/${tournamentId}`))
    );
  }

  acknowledgeBracketUpdate(tournamentBracketUpdateId: number):  Observable<any> {
    return this.http.put(`${this.apiUrl}/gameUpdates/${tournamentBracketUpdateId}`, null);
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

  updateResult(resultId: string, request: ISaveGameResultRequest): Observable<IGameResult> {
    return this.http.put<IGameResult>(`${this.apiUrl}/${resultId}`, request);
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
}
