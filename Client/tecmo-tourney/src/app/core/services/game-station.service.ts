import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ConfigService } from './config.service';
import { IGameStationGamesResponse } from '../models/game-station-games-response.model';
import { IGameStationUpdateRequest } from '../models/request/game-station-update-request.model';
import { IGameResult } from '../models/gameResult.model';

@Injectable({
  providedIn: 'root'
})
export class GameStationService {
  private readonly baseUrl: string;

  constructor(private http: HttpClient, private configService: ConfigService) {
    this.baseUrl = `${this.configService.getApiUrl()}/game-station`;
  }

  getGames(): Observable<IGameStationGamesResponse> {
    return this.http.get<IGameStationGamesResponse>(`${this.baseUrl}/games`);
  }

  updateGame(gameResultId: number, body: IGameStationUpdateRequest): Observable<IGameResult> {
    return this.http.put<IGameResult>(`${this.baseUrl}/games/${gameResultId}`, body);
  }
}
