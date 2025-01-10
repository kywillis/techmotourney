import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { interval, Observable, switchMap } from 'rxjs';
import { ITournamentBracketUpdate } from '../models/tournamentBracketUpdate.model'
import { ConfigService } from './config.service';

@Injectable({
  providedIn: 'root'
})
export class TournamentBracketUpdateService {
  private apiUrl: string;

  constructor(private http: HttpClient, private configService: ConfigService) {
    this.apiUrl = this.configService.getApiUrl();
  }
  
  getResultsPolling(intervalSeconds: number, tournamentId: number): Observable<ITournamentBracketUpdate[]> {
    return interval(intervalSeconds * 1000).pipe(
      switchMap(() => this.http.get<ITournamentBracketUpdate[]>(`${this.apiUrl}/tournamentsBracketUpdate?tournamentID=${tournamentId}`))
    );
  }
}
