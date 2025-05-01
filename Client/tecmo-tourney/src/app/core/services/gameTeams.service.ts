import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { shareReplay, tap } from 'rxjs/operators';
import { IGameTeam } from '../models/gameTeam.model';
import { ConfigService } from './config.service';

@Injectable({
  providedIn: 'root'
})
export class GameTeamsService {
  private teamsCache$: Observable<any[]> | null = null;
  private apiUrl: string;

  constructor(private http: HttpClient, private configService: ConfigService) {
    this.apiUrl = this.configService.getApiUrl();
  }

  getAll(): Observable<IGameTeam[]> {
    const cached = localStorage.getItem('gameTeams');
    if(cached){
      return of(JSON.parse(cached)); 
    }
    return this.http.get<IGameTeam[]>(`${this.apiUrl}/gameTeams`).pipe(
      tap(data => localStorage.setItem('gameTeams', JSON.stringify(data)))
    );
  }
}