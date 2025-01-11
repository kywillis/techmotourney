import { Component, OnInit, ViewChild } from '@angular/core';
import { TournamentsService } from 'src/app/core/services/tournaments.service';
import { PlayersService } from 'src/app/core/services/players.service';
import { ResultsService } from 'src/app/core/services/results.service';
import { ActivatedRoute } from '@angular/router';
import { ITournament } from 'src/app/core/models/tournament.model';
import { IPlayer } from 'src/app/core/models/player.model';
import { forkJoin, Subscription } from 'rxjs';
import { IGameResult } from 'src/app/core/models/gameResult.model';
import { GameStatus, GameType, TournamentStatus } from 'src/app/enums';
import { ModalComponent } from 'src/app/shared/components/modal/modal.component';
import { EditGameResultComponent } from 'src/app/shared/components/edit-game-result/edit-game-result.component';
import { DeleteGameResultComponent } from 'src/app/shared/components/delete-game-result/delete-game-result.component';
import { ITournamentStanding } from 'src/app/core/models/tournamentStandingModel';
import { IChangeTournamentStatusRequest } from 'src/app/core/models/request/changeTournamentStatusRequest.model';
import { ISaveGameResultRequest } from 'src/app/core/models/request/saveGameResultRequest.model';
import { IGameResultPlayer } from 'src/app/core/models/gameResultPlayer.model';
import { TournamentBracketUpdateService } from 'src/app/core/services/tournamentBracketUpdate.service';
declare const $: any;

@Component({
  selector: 'app-view-tournament',
  templateUrl: './view-tournament.component.html',
  styleUrls: ['./view-tournament.component.less']
})
export class ViewTournamentComponent implements OnInit { 
  @ViewChild('deleteGameResultModal') deleteGameResultModal!: ModalComponent;
  @ViewChild('editGameResultModal') editGameResultModal!: ModalComponent;
  @ViewChild('editGameResult') editGameResult!: EditGameResultComponent;
  @ViewChild('deleteGameResult') deleteGameResult!: DeleteGameResultComponent;
  selectedIndex = 0;
  tournament?: ITournament
  players: IPlayer[] = [];
  prelimGames: IGameResult[] = [];
  tournamentGames: IGameResult[] = [];
  standings: ITournamentStanding[] = [];
  TournamentStatus = TournamentStatus;
  tournamentUpdatesSubscription: Subscription | null = null;

  loading = false;
  constructor(private tournamentService: TournamentsService, 
    private playersService: PlayersService, 
    private resultService: ResultsService, 
    private route: ActivatedRoute,
    private tournamentBracketUpdateService: TournamentBracketUpdateService) { }

  ngOnInit(): void {
    this.loading = true;
    let tournamentId = +this.route.snapshot.paramMap.get('id')!; 
    forkJoin({
      tournament: this.tournamentService.getTournament(tournamentId),
      players: this.playersService.getPlayers(tournamentId)
    }).subscribe(({ tournament, players }) => {
      this.tournament = tournament;
      this.players = players;
      this.loading = false;
    
      if(this.tournament.status == TournamentStatus.Preliminaries || this.tournament.status == TournamentStatus.Tournament)
      {
        this.selectedIndex = 0;
        this.loadGames();

        if(this.tournament.status == TournamentStatus.Tournament)
          this.loadBrackets(this.tournament!.tournamentBracket);        
      }     
    });

    window.addEventListener('message', (event) => {
      console.log(JSON.stringify(event.data));
      console.log(event.data);
    });
  }

  startPrelims(): void{
    var statusRequest = {
      status: TournamentStatus.Preliminaries,
      tournamentId: this.tournament!.tournamentId
    } as IChangeTournamentStatusRequest;
   this.tournamentService.setStatus(statusRequest).subscribe((tournament)=>{
    this.tournament = tournament
    this.loadGames();
   })
  }

  loadGames(){
    this.loading = true;
  
    forkJoin(
      {
        games: this.resultService.getResultsByTournmanentId(this.tournament!.tournamentId),
        standings: this.tournamentService.getTournamentStandings(this.tournament!.tournamentId, this.tournament!.status)
      }).subscribe(({games, standings}) => {
        this.loading = false;        
        this.prelimGames = games.filter(game => game.gameType === GameType.Preliminary);
        this.tournamentGames = games.filter(game => game.gameType === GameType.Tournament);
        this.standings = standings;
      });      
  }

  gameResultSaved(){
    this.editGameResultModal.close();
    this.loadGames();
  }

  gameDeleted(){
    this.loadGames();
  }

  onEditGame(gameResult: IGameResult | null) {
    if(gameResult)
      this.editGameResult.setGame(gameResult!, this.players); 
    else{
      let newGame = {
        tournamentId: this.tournament!.tournamentId,        
      } as IGameResult;
      this.editGameResult.setGame(newGame, this.players); 
    }
    this.editGameResultModal.open();
  }

  onDeleteGame(gameResult: IGameResult) {
    this.deleteGameResult.setGame(gameResult);
    this.deleteGameResultModal.open();
  }

  startTournament() {
      var statusRequest : IChangeTournamentStatusRequest = {
        status: TournamentStatus.Tournament,
        tournamentId: this.tournament!.tournamentId,
        newGames: [],
        bracketData: {}
      };

      this.tournamentService.setStatus(statusRequest).subscribe((tournament)=>{
        this.tournament = tournament  
        this.loadGames();
      })
  }

  onTabChange(index: number){
    if(index == 1 && this.tournament != null){
      this.loadBrackets(this.tournament.tournamentBracket);    
    }
  }

  loadBrackets(viewerData: any){    
    setTimeout(() => {
      const iframe = document.getElementById('bracket-iframe') as HTMLIFrameElement;
      if (iframe && iframe.contentWindow) {
        console.log('send message');
        iframe.contentWindow.postMessage({ type: 'initBracket', data: viewerData }, '*');        
      }
      }, 1000);
  }
}  