import { Component, OnInit, AfterViewInit, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ITournament } from 'src/app/core/models/tournament.model';
import { TournamentsService } from 'src/app/core/services/tournaments.service';
import { EditGameResultComponent } from 'src/app/shared/components/edit-game-result/edit-game-result.component';
import { IGameResult } from 'src/app/core/models/gameResult.model';
import { GameStatus, GameType, TournamentStatus } from 'src/app/enums';
import { IGameResultPlayer } from 'src/app/core/models/gameResultPlayer.model';

@Component({
  selector: 'app-submit-game',
  standalone: false,
  templateUrl: './submit-game-result.component.html',
  styleUrl: './submit-game-result.component.less'
})
export class SubmitGameResultComponent implements OnInit, AfterViewInit  {
  gameSaved: boolean = false;
  tournament?: ITournament
  tournamentId: number = 0;
  gameResults: IGameResult[] = [];
  @ViewChild('createGameResult') createGameResult!: EditGameResultComponent;

  constructor(
    private tournamentService: TournamentsService,
    private route: ActivatedRoute){
  }

  ngOnInit(): void {
    
  }

  ngAfterViewInit(): void {
    setTimeout(()=>{
      this.tournamentService.getActiveTournament().subscribe(tournament =>{
        this.tournament = tournament;
        this.tournamentId = tournament.tournamentId;
        this.buildGameResultsFromQueryString();
      });
    }, 1000);
  }

  buildGameResultsFromQueryString(){
    this.route.queryParams.subscribe(params => {
      const player1: IGameResultPlayer = {
        playerName: params['team1'],
        teamName: params['team1'],
        playerId: 0,
        gameTeamId: null,
        score: +params['score1'],
        passingYards: +params['passingYards1'],
        rushingYards: +params['rushingYards1']
      };

      const player2: IGameResultPlayer = {
        playerName: params['team2'],
        teamName: params['team2'],
        playerId: 0,
        gameTeamId: null,
        score: +params['score2'],
        passingYards: +params['passingYards2'],
        rushingYards: +params['rushingYards2']
      };

      const gameResult: IGameResult = {
        gameResultId: 0,
        tournamentId: this.tournament ? this.tournament.tournamentId : 0,
        player1,
        player2,
        date: new Date(),
        status: GameStatus.Completed,
        gameType: (this.tournament?.status == TournamentStatus.Preliminaries) ? GameType.Preliminary : GameType.Tournament
      };

      this.gameResults = [...this.gameResults, gameResult];
    });
  }

  gameResultSaved(){
    this.gameSaved = true;
  }

}
