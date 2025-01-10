import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { ITournament } from '../models/tournament.model';
import { ISaveTournamentRequest } from '../models/request/saveTournamentRequest.model'
import { ConfigService } from './config.service';
import { ITournamentStanding } from '../models/tournamentStandingModel';
import { TournamentStatus } from 'src/app/enums';
import { IChangeTournamentStatusRequest } from '../models/request/changeTournamentStatusRequest.model';

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

  getAllTournament(): Observable<ITournament[]> {
    return this.http.get<ITournament[]>(`${this.apiUrl}/tournaments`);
  }

  getTournament(tournamentId: number): Observable<ITournament> {
    return this.http.get<ITournament>(`${this.apiUrl}/tournaments/${tournamentId}`).pipe(
      map((tournament: ITournament) => {
        tournament.tournamentBracket = JSON.parse(JSON.stringify(tournament.tournamentBracket));
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
}
