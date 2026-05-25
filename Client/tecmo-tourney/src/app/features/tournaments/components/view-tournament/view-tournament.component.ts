import { Component, OnDestroy, OnInit, ViewChild  } from '@angular/core';
import { TournamentsService } from 'src/app/core/services/tournaments.service';
import { PlayersService } from 'src/app/core/services/players.service';
import { ResultsService } from 'src/app/core/services/results.service';
import { ActivatedRoute } from '@angular/router';
import { ITournament } from 'src/app/core/models/tournament.model';
import { IPlayer } from 'src/app/core/models/player.model';
import { delay, forkJoin, interval, Subscription } from 'rxjs';
import { IGameResult } from 'src/app/core/models/gameResult.model';
import { StatType, GameType, TournamentStatus, GameStatus } from 'src/app/enums';
import { DisplayStatsComponent } from '../display-stats/display-stats.component';
import { ModalComponent } from 'src/app/shared/components/modal/modal.component';
import { EditGameResultComponent } from 'src/app/shared/components/edit-game-result/edit-game-result.component';
import { DeleteGameResultComponent } from 'src/app/shared/components/delete-game-result/delete-game-result.component';
import { ViewGameResultComponent } from 'src/app/shared/components/view-game-result/view-game-result.component';
import { ITournamentStanding } from 'src/app/core/models/tournamentStandingModel';
import { IChangeTournamentStatusRequest } from 'src/app/core/models/request/changeTournamentStatusRequest.model';
import { MatTabGroup } from '@angular/material/tabs';
import { TournamentBracketUpdateService } from 'src/app/core/services/tournamentBracketUpdate.service';
import { IGameSearchParameters } from 'src/app/core/models/gameSearchParameters';
import { DatePipe } from '@angular/common';
import { GoogleAuthService } from 'src/app/core/services/google-auth.service';
import { IResetTournamentRequest } from 'src/app/core/models/request/resetTournamentRequest.model';
declare const $: any;

@Component({
    selector: 'app-view-tournament',
    templateUrl: './view-tournament.component.html',
    styleUrls: ['./view-tournament.component.less'],
    standalone: false
})
export class ViewTournamentComponent implements OnInit, OnDestroy  {
  @ViewChild('resetModal') resetModal!: ModalComponent;
  @ViewChild('deleteGameResultModal') deleteGameResultModal!: ModalComponent;
  @ViewChild('editGameResultModal') editGameResultModal!: ModalComponent;
  @ViewChild('viewGameResultModal') viewGameResultModal!: ModalComponent;
  @ViewChild('viewGameResultsModal') viewGameResultsModal!: ModalComponent;
  @ViewChild('editGameResult') editGameResult!: EditGameResultComponent;
  @ViewChild('deleteGameResult') deleteGameResult!: DeleteGameResultComponent;
  @ViewChild('viewGameResult') viewGameResult!: ViewGameResultComponent;
  @ViewChild('stats') stats!: DisplayStatsComponent;

  @ViewChild(MatTabGroup) tabGroup!: MatTabGroup;
  filteredPlayerName = 'Prelim';
  statType = StatType;
  selectedIndex = 0;
  tournament?: ITournament
  players: IPlayer[] = [];
  prelimGames: IGameResult[] = [];
  filteredPrelimGames: IGameResult[] = [];
  allGames: IGameResult[] = [];
  selectedGames: IGameResult[] = [];
  standings: ITournamentStanding[] = [];
  TournamentStatus = TournamentStatus;
  tournamentUpdatesSubscription: Subscription | null = null;
  fetchSubscription!: Subscription;
  private adminLoggedSubscription?: Subscription;
  selectedStatType : StatType = StatType.HighestScore;
  tournamentCompleted: boolean = false;
  resetError: string = '';
  showResetControls: boolean = false;
  loading = false;

  constructor(private tournamentService: TournamentsService, 
    private playersService: PlayersService, 
    private resultService: ResultsService, 
    private route: ActivatedRoute,
    private tournamentBracketUpdateService: TournamentBracketUpdateService,
    private datePipe: DatePipe, 
    private googleAuth: GoogleAuthService) { }

  ngOnInit(): void {
    this.loading = true;
    this.loadTournament();

    window.addEventListener('message', (event) => {
      if(event.data.messageType == "bracketUpdate")
      {
        this.tournament!.bracketData = event.data.payload.bracketData;

        for (let i = 0; i < event.data.payload.pointSpreadMatchUps.length; i++) {
          event.data.payload.pointSpreadMatchUps[i].tournamentId = this.tournament!.tournamentId;          
        }

        this.resultService.createPointSpreads(this.tournament!.tournamentId, event.data.payload.pointSpreadMatchUps).subscribe({
          next: (result)=>{
            this.loadPointSpreads();
          }
        })
        this.tournamentService.updateTournamentBrackets(this.tournament!.tournamentId, this.tournament!.bracketData).subscribe({
          next: (result)=>{
            console.log('bracket updated')           
          }
        })
      }
      else if(event.data.messageType == "gameSelected"){
        console.log('show game:' + event.data.payload);
        this.viewGame(event.data.payload);
      }
    });

    this.googleAuth.isAdminLoggedIn$.subscribe(val => {
      this.sendBracketMessage("setAdmin", val);
    });

    interval(30 * 1000).subscribe(() => { //get updates to games and standings every 30 seconds      
        this.loadGames();      
    });
  }

  ngOnDestroy(): void {
    if (this.fetchSubscription) {
      this.fetchSubscription.unsubscribe();
    }
    this.adminLoggedSubscription?.unsubscribe();
  }

  startPrelims(): void{
    this.loading = true;
    var statusRequest = {
      status: TournamentStatus.Preliminaries,
      tournamentId: this.tournament!.tournamentId
    } as IChangeTournamentStatusRequest;
   this.tournamentService.setStatus(statusRequest).subscribe((tournament)=>{
    this.tournament = tournament
    this.loadGames();
    this.loading = false;
   })
  }

  loadTournament(){
    this.showResetControls = false;
    this.loading = true;
    let tournamentId = +this.route.snapshot.paramMap.get('id')!; 
    
    this.fetchSubscription = this.resultService.getBracketUpdates(tournamentId).subscribe({
      next: async (data) => {
        for (let i = 0; i < data.length; i++) {
            this.sendBracketMessage('result', data[i]);
            await delay(1200);
            if(this.tabGroup && this.tabGroup.selectedIndex == 1){
              this.resultService.acknowledgeBracketUpdate(data[i].tournamentBracketUpdateId).subscribe({
                next: (result)=>{console.log(`acknowledged ${data[i].tournamentBracketUpdateId}`)}
              });
            }
        }
      },
      error: (err) => console.error('Error:', err)
    });

    forkJoin({
      tournament: this.tournamentService.getTournament(tournamentId),
      players: this.playersService.getPlayers(tournamentId)
    }).subscribe(({ tournament, players }) => {
      this.tournament = tournament;
      this.players = players;
      this.loading = false;
    
      if(this.tournament.status == TournamentStatus.Deleted || this.tournament.status == TournamentStatus.Waiting)
        return;

      this.loadGames();
      this.loadPointSpreads();

      if(this.tournament.status == TournamentStatus.Preliminaries)      
        this.selectedIndex = 0;
      
      if(this.tournament.status == TournamentStatus.Tournament || this.tournament.status == TournamentStatus.Completed){
        this.selectedIndex = 1;
        this.sendBracketMessage('initBracket', this.tournament!.bracketData);               
      }     

      const savedIndex = localStorage.getItem('selectedTabIndex');
      if (savedIndex !== null) {
        this.selectedIndex = +savedIndex;
      }

      this.tournamentCompleted = this.tournament.status == TournamentStatus.Completed;
    });
  }

  loadGames(){
    forkJoin(
      {
        games: this.resultService.getResultsByTournmanentId(this.tournament!.tournamentId),
        standings: this.tournamentService.getTournamentStandings(this.tournament!.tournamentId, this.tournament!.status)
      }).subscribe(({games, standings}) => {
        this.loading = false;        
        this.filteredPrelimGames = this.prelimGames = games.filter(game => game.gameType === GameType.Preliminary);
        this.allGames = games;//.filter(game => game.status == GameStatus.Completed);
        this.standings = standings;
      });      
  }

  loadPointSpreads(){
    this.resultService.getPointSpreads(this.tournament!.tournamentId).subscribe((results) => {
      if(results.length > 0){
        this.sendBracketMessage("pointSpreads", results);
      }
    })
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
      this.tournament.bracketData = JSON.parse(this.tournament.bracketData);
      this.loadGames();
    })
  }

  onTabChange(index: number){
    this.selectedIndex = index;
    localStorage.setItem('selectedTabIndex', index.toString());
    
    if(index == 1 && this.tournament != null){
      this.sendBracketMessage('initBracket', this.tournament.bracketData);    
    }
  }

  filterPrelimGames(playerId: number){
    if(playerId < 1){
      this.filteredPlayerName = 'Prelim';
      this.filteredPrelimGames = this.prelimGames;
    }
    else {
      this.filteredPlayerName = this.players.filter(p => p.playerId == playerId)[0].fullName + ' ';
      this.filteredPrelimGames = this.prelimGames.filter(g => g.player1.playerId == playerId || g.player2.playerId == playerId);
    }
  }

  sendBracketMessage(type: string, data: any){    
    console.log('new bracket update')
    console.log(data)
    setTimeout(() => {
      const iframe = document.getElementById('bracket-iframe') as HTMLIFrameElement;
      if (iframe && iframe.contentWindow) {
        iframe.contentWindow.postMessage({ type: type, data: data }, '*');        
      }
      }, 1000);
  }

  showGames(games: IGameResult[]){
    this.selectedGames = games;
    this.viewGameResultsModal.open();
  }

  viewGame(gameSearchParameters:IGameSearchParameters){
    gameSearchParameters.tournamentId = this.tournament!.tournamentId;
    this.resultService.searchResulsts(gameSearchParameters).subscribe((gameResult)=>{
      this.viewGameResult.gameResult = gameResult[0];
      this.viewGameResultModal.title = this.datePipe.transform(gameResult[0].date, 'h:mma M/d/yy') || '';
      this.viewGameResultModal.open();
    });
  }

  loggedIn(): boolean {
    return this.googleAuth.isAdminLoggedIn();
  }

  showReset(): void {
    this.resetError = '';
    this.resetModal.open();
  }

  resetTournament(): void {
    const request: IResetTournamentRequest = {
      tournamentId: this.tournament!.tournamentId
    };

    this.resetError = '';
    this.tournamentService.resetTournament(this.tournament!.tournamentId, request).subscribe({
      next: () => {
        this.resetModal.close();
        this.loadTournament();
      },
      error: () => {
        this.resetError = 'Reset failed.';
      }
    });
  }
}  
