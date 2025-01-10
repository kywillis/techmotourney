import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { IPlayer } from '../models/player.model';
import { CreatePlayerRequest } from '../models/request/createPlayerRequest.model';
import { ConfigService } from './config.service';

@Injectable({
  providedIn: 'root'
})
export class PlayersService {
  private apiUrl: string;

  constructor(private http: HttpClient, private configService: ConfigService) {
    this.apiUrl = this.configService.getApiUrl();
  }

  createPlayer(request: CreatePlayerRequest): Observable<IPlayer> {
    return this.http.post<IPlayer>(`${this.apiUrl}/players`, request);
  }
  getAllPlayers(): Observable<IPlayer[]> {
    return this.http.get<IPlayer[]>(`${this.apiUrl}/players`);
  }

  getPlayer(playerId: number): Observable<IPlayer> {
    return this.http.get<IPlayer>(`${this.apiUrl}/players/${playerId}`);
  }

  getPlayers(tournamentId: number): Observable<IPlayer[]> {
    return this.http.get<IPlayer[]>(`${this.apiUrl}/players/tournament/${tournamentId}`);
  }

  updatePlayer(playerId: number, request: CreatePlayerRequest): Observable<IPlayer> {
    return this.http.put<IPlayer>(`${this.apiUrl}/players/${playerId}`, request);
  }

  deletePlayer(playerId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/players/${playerId}`);
  }
}