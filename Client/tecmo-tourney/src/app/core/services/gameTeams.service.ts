import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { IGameTeam } from '../models/gameTeam.model';
import { ConfigService } from './config.service';

@Injectable({
  providedIn: 'root'
})
export class GameTeamsService {
  private apiUrl: string;

  constructor(private http: HttpClient, private configService: ConfigService) {
    this.apiUrl = this.configService.getApiUrl();
  }

  getAll(): Observable<IGameTeam[]> {
    return this.http.get<IGameTeam[]>(`${this.apiUrl}/gameTeams`);
  }
}