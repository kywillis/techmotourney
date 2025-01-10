import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { IGameResult } from '../models/gameResult.model';
import { ISaveGameResultRequest } from '../models/request/saveGameResultRequest.model';
import { ConfigService } from './config.service';

@Injectable({
  providedIn: 'root'
})
export class ResultsService {
  private apiUrl: string;

  constructor(private http: HttpClient, private configService: ConfigService) {
    this.apiUrl = this.configService.getApiUrl();
  }

  createResult(request: ISaveGameResultRequest): Observable<IGameResult> {
    return this.http.post<IGameResult>(`${this.apiUrl}/results/`, request);
  }

  getResult(resultId: number): Observable<IGameResult> {
    return this.http.get<IGameResult>(`${this.apiUrl}/results/${resultId}`);
  }

  getResultsByTournmanentId(tournmanentId: number): Observable<IGameResult[]> {
    return this.http.get<IGameResult[]>(`${this.apiUrl}/results/tournament/${tournmanentId}`);
  }

  updateResult(resultId: string, request: ISaveGameResultRequest): Observable<IGameResult> {
    return this.http.put<IGameResult>(`${this.apiUrl}/results/${resultId}`, request);
  }

  deleteResult(gameResultId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/results/${gameResultId}`);
  }
}
