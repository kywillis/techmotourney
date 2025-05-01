import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable} from 'rxjs';
import { ITournament } from '../models/tournament.model';
import { ISaveTournamentRequest } from '../models/request/saveTournamentRequest.model'
import { ConfigService } from './config.service';
import { ITournamentStanding } from '../models/tournamentStandingModel';
import { TournamentStatus } from 'src/app/enums';
import { IChangeTournamentStatusRequest } from '../models/request/changeTournamentStatusRequest.model';
import { IResetTournamentRequest } from '../models/request/resetTournamentRequest.model';

@Injectable({
  providedIn: 'root'
})
export class TournamentsService {
  private apiUrl: string;

  constructor(private http: HttpClient, private configService: ConfigService) {
    this.apiUrl = this.configService.getApiUrl();
  }
  
  createTournament(request: ISaveTournamentRequest): Observable<ITournament> {
    return this.http.post<ITournament>(`${this.apiUrl}/tournaments`, request);
  }

  getAllTournaments(): Observable<ITournament[]> {
    return this.http.get<ITournament[]>(`${this.apiUrl}/tournaments`);
  }

  getActiveTournament(): Observable<ITournament> {
    return this.http.get<ITournament>(`${this.apiUrl}/tournaments/active`).pipe(
      map((tournament: ITournament) => {
        if(tournament.bracketData && tournament.bracketData != '')
          tournament.bracketData = JSON.parse(tournament.bracketData);
        return tournament;
      })
    );
  }

  getTournament(tournamentId: number): Observable<ITournament> {
    return this.http.get<ITournament>(`${this.apiUrl}/tournaments/${tournamentId}`).pipe(
      map((tournament: ITournament) => {
        if(tournament.bracketData != '')
          tournament.bracketData = JSON.parse(tournament.bracketData);
        return tournament;
      })
    );
  }

  updateTournament(request: ISaveTournamentRequest): Observable<ITournament> {
    return this.http.put<ITournament>(`${this.apiUrl}/tournaments/${request.tournamentId}`, request);
  }

  updateTournamentBrackets(tournamentId: number, bracketDate: any): Observable<any> {
    return this.http.patch<ITournament>(`${this.apiUrl}/tournaments/${tournamentId}`, bracketDate);
  }

  deleteTournament(tournamentId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/tournaments/${tournamentId}`);
  }

  setStatus(statusRequest: IChangeTournamentStatusRequest): Observable<ITournament>{
    return this.http.put<ITournament>(`${this.apiUrl}/tournaments/${statusRequest.tournamentId}/status`, statusRequest);
  }

  getTournamentStandings(tournamentId: number, status: TournamentStatus): Observable<ITournamentStanding[]> {
    return this.http.get<ITournamentStanding[]>(`${this.apiUrl}/tournaments/${tournamentId}/standings?status=${status}`);
  }

  resetTournament(tournamentId: number, request: IResetTournamentRequest): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/tournaments/${tournamentId}/reset`, request);
  }
}
