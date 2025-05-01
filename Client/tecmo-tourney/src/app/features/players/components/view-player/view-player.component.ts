import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { forkJoin, Subscription } from 'rxjs';
import { IGameResult } from 'src/app/core/models/gameResult.model';
import { IGameResultPlayer } from 'src/app/core/models/gameResultPlayer.model';
import { IPlayer } from 'src/app/core/models/player.model';
import { IOpponentMatchUpResult } from 'src/app/core/models/opponentMatchupResult.model';
import { PlayersService } from 'src/app/core/services/players.service';
import { ResultsService } from 'src/app/core/services/results.service';
import { ITeamHistoryResult } from 'src/app/core/models/teamHistory.model';
import { TournamentsService } from 'src/app/core/services/tournaments.service';
import { ITournament } from 'src/app/core/models/tournament.model';
import { IGameSearchParameters } from 'src/app/core/models/gameSearchParameters';
import { ModalComponent } from 'src/app/shared/components/modal/modal.component';
import { PlayerStatDetailsType } from 'src/app/enums';
import { AuthenticationService } from 'src/app/core/services/authentication.service';

@Component({
    selector: 'app-view-player',
    templateUrl: './view-player.component.html',
    styleUrl: './view-player.component.less',
    standalone: false
})
export class ViewPlayerComponent implements OnInit, OnDestroy{
  @ViewChild('resultsModal') resultsModal!: ModalComponent;
  
  playerStatDetailsType = PlayerStatDetailsType

  readonly BAD: string = "Bad";
  readonly AVERAGE: string = "Average";
  readonly GOOD: string = "Good";
  readonly EXCELLENT: string = "Excellent";

  selectedStatType : PlayerStatDetailsType = PlayerStatDetailsType.Opponent;
  private paramSubscription: Subscription | undefined;

  modalTitle: string = "";
  minMatchupsThreshold: number = 0;
  tournamentId: number | null = null;
  loading: boolean = false;
  player?: IPlayer;
  results: IGameResult[] = [];
  filteredresults: IGameResult[] = [];
  tournaments: ITournament[] = [];
  selectedTournamentId: number = -1;

  condition: string = 'bad'
  record: string = "0-0";
  avgRushYards: number = 0;
  avgPassYards: number = 0;
  avgPtsAgsint: number = 0;
  avgPtsFor: number = 0;

  bestMatchups: IOpponentMatchUpResult[] = [];
  worstMatchups: IOpponentMatchUpResult[] = [];
  teamPlayedWith: ITeamHistoryResult[] = [];
  teamPlayedAgainst: ITeamHistoryResult[] = [];

  constructor(
    private playersService: PlayersService,
    private resultsService: ResultsService,
    private route: ActivatedRoute,
    private tournamentService: TournamentsService, 
    private authenticationService: AuthenticationService) { }

  ngOnInit(): void {    
    this.paramSubscription = this.route.paramMap.subscribe(params => {
      if(this.resultsModal)
        this.resultsModal.close();

      const playerId = Number(params.get('id'));
      this.loadPlayer(playerId);      
    });

    this.tournamentService.getAllTournaments()
      .subscribe(tournaments => {this.tournaments = tournaments});
  }

  ngOnDestroy() {
    if (this.paramSubscription) {
      this.paramSubscription.unsubscribe();
    }
  }

  loadPlayer(playerId: number) {
    this.loading = true;
    
    let searchParams = {
      tournamentId: this.tournamentId,
      player1ID: playerId,
      player2ID: null,
      matchupLocation: null
    } as IGameSearchParameters;

    forkJoin({
      results: this.resultsService.searchResulsts(searchParams),
      player: this.playersService.getPlayer(searchParams.player1ID!)
    }).subscribe(({ results, player}) => {
      this.loading = false;
      this.results = results;
      this.player = player;
      this.results.sort((a, b) => {
        const dateA = new Date(b.date).getTime(); // Use getTime()
        const dateB = new Date(a.date).getTime(); // Use getTime()
        return dateA - dateB;
      });
      
      this.calculateStats(this.results);
    });
  }

  showStatDetails(targetId: number, detailsType: PlayerStatDetailsType, label: string){
    this.filteredresults = [];

    for (let i = 0; i < this.results.length; i++) {
      switch (detailsType) {
        case PlayerStatDetailsType.Opponent:
          this.modalTitle = `Matchups Against ${label}`;
          if(this.results[i].player1.playerId == targetId || this.results[i].player2.playerId == targetId)
            this.filteredresults.push(this.results[i]);
          break;
        case PlayerStatDetailsType.TeamPlayedAgainst:
          this.modalTitle = `Games Playing Against ${label}`;
          if((this.results[i].player1.gameTeamId == targetId && this.results[i].player2.playerId == this.player!.playerId) ||
             (this.results[i].player2.gameTeamId == targetId && this.results[i].player1.playerId == this.player!.playerId) )
             this.filteredresults.push(this.results[i]);
          break;
        case PlayerStatDetailsType.TeamPlayedWith:
          this.modalTitle = `Games Playing With ${label}`;
          if((this.results[i].player1.gameTeamId == targetId && this.results[i].player1.playerId == this.player!.playerId) ||
             (this.results[i].player2.gameTeamId == targetId && this.results[i].player2.playerId == this.player!.playerId) )
             this.filteredresults.push(this.results[i]);
          break;
        default:
          break;
      }
    }

    this.resultsModal.open();
  }

  calculateStats(results:IGameResult[]) {
    this.calculateCondition(results);
    let wins = results.filter(g => this.calculateResult(g, this.player!.playerId) > 0);
    let losses = results.filter(g => this.calculateResult(g, this.player!.playerId) < 0);

    this.record = `${wins.length} - ${losses.length}`;

    let allPlayerStats = results.flatMap(g => this.getPlayerStats(g, this.player!.playerId));
    this.avgPassYards = Math.round(this.calculateAverage(allPlayerStats, 'passingYards'));
    this.avgRushYards = Math.round(this.calculateAverage(allPlayerStats, 'rushingYards'));
    this.avgPtsFor = Math.round(this.calculateAverage(allPlayerStats, 'score'));

    let allOpponentStats = results.flatMap(g => this.getPlayerStats(g, this.player!.playerId, true));
    this.avgPtsAgsint = Math.round(this.calculateAverage(allOpponentStats, 'score'));

    this.calculateMatchups(results);
    this.calculateTeamsPlayedWith(results);
    this.calculateTeamsPlayedAgainst(results);
  }

  calculateTeamsPlayedWith(results: IGameResult[]) {
    let withIndex = -1;
    this.teamPlayedWith = [];

    for (let i = 0; i < results.length; i++) {
      let win = this.calculateResult(results[i], this.player!.playerId);
      let playerStat = this.getPlayerStats(results[i], this.player!.playerId);
      let opponentStat = this.getPlayerStats(results[i], this.player!.playerId, true);
      let withTeam : ITeamHistoryResult;
      withIndex = this.teamPlayedWith.findIndex(t => t.teamName == playerStat.teamName);
      
      if(withIndex < 0){
        withTeam = { teamId: playerStat.gameTeamId!, avgDiff: 0, losses: 0, wins:0, pointsAgainst: 0, pointsFor: 0, teamName: playerStat.teamName };
        this.teamPlayedWith.push(withTeam);
      }
      else 
        withTeam = this.teamPlayedWith[withIndex];
      
      if(win > 0)
        withTeam.wins++;
      else 
        withTeam.losses++;

        withTeam.pointsAgainst += opponentStat.score;
        withTeam.pointsFor += playerStat.score;
        withTeam.avgDiff = Math.round((withTeam.pointsFor - withTeam.pointsAgainst)/(withTeam.wins + withTeam.losses));
    }
    this.teamPlayedWith.sort((a, b) => {
      const totalA = a.wins + a.losses;
      const totalB = b.wins + b.losses;
      return totalB - totalA; // Sort in descending order (highest total first)
    });
  }

  calculateTeamsPlayedAgainst(results: IGameResult[]) {
    let againstIndex = -1;
    this.teamPlayedAgainst = [];

    for (let i = 0; i < results.length; i++) {
      let win = this.calculateResult(results[i], this.player!.playerId);
      let playerStat = this.getPlayerStats(results[i], this.player!.playerId);
      let opponentStat = this.getPlayerStats(results[i], this.player!.playerId, true);
      let againstTeam : ITeamHistoryResult;
      againstIndex = this.teamPlayedAgainst.findIndex(t => t.teamName == opponentStat.teamName);
      
      if(againstIndex < 0){
        againstTeam = {teamId: opponentStat.gameTeamId!,  avgDiff: 0, losses: 0, wins:0, pointsAgainst: 0, pointsFor: 0, teamName: opponentStat.teamName };
        this.teamPlayedAgainst.push(againstTeam);
      }
      else 
        againstTeam = this.teamPlayedAgainst[againstIndex];
      
      if(win > 0)
        againstTeam.wins++;
      else 
        againstTeam.losses++;

      againstTeam.pointsAgainst += opponentStat.score;
      againstTeam.pointsFor += playerStat.score;
      againstTeam.avgDiff = Math.round((againstTeam.pointsFor - againstTeam.pointsAgainst)/(againstTeam.wins + againstTeam.losses));
    }

    this.teamPlayedAgainst.sort((a, b) => {
      const totalA = a.wins + a.losses;
      const totalB = b.wins + b.losses;
      return totalB - totalA; // Sort in descending order (highest total first)
    });
  }

  calculateCondition(results: IGameResult[]) {
    if (results.length == 0) {
      this.condition = 'NA'
      return;
    }

    var last3Games = (results.length < 3) ? results : results.slice(0, 3);
    var wins = 0
    for (let i = 0; i < last3Games.length; i++) {
      wins += this.calculateResult(last3Games[i], this.player!.playerId);
    }

    switch (wins) {
      case 0:
        this.condition = this.BAD;
        break;
      case 1:
        this.condition = this.AVERAGE;
        break;
      case 2:
        this.condition = this.GOOD;
        break;
      case 3:
        this.condition = this.EXCELLENT;
        break;
      default:
        break;
    }
  }

  calculateResult(game: IGameResult, playerId: number): number {
    if ((game.player1.score > game.player2.score && game.player1.playerId == playerId) ||
      (game.player2.score > game.player1.score && game.player2.playerId == playerId))
      return 1;
    else
      return -1;
  }

  getPlayerStats(game:IGameResult, playerId: number, opponent: boolean = false): IGameResultPlayer{
    if(!opponent) //we want the player passed in
      return (game.player1.playerId == playerId) ? game.player1 : game.player2;
    else //we want the opponent
      return (game.player1.playerId != playerId) ? game.player1 : game.player2;
  }

  calculateAverage<T>(array: T[], property: keyof T): number {
    if (array.length === 0) {
      return 0;
    }
  
    const sum = array.reduce((acc, item) => {
      const value = item[property];
      if (typeof value === 'number') {
        return acc + value;
      } else {
        console.warn(`Property '${String(property)}' is not a number for item:`, item);
        return acc; 
      }
    }, 0);
  
    return sum / array.length;
  }

  calculateMatchups(results:IGameResult[]){
    let allMatchups: IOpponentMatchUpResult[] = [];
    this.bestMatchups = [];
    this.worstMatchups = [];

    for (let i = 0; i < results.length; i++) {
      let opponentResults = this.getPlayerStats(results[i], this.player!.playerId, true);
      let playerResults = this.getPlayerStats(results[i], this.player!.playerId, false);
      let matchUp = allMatchups.find(m => m.opponentId == opponentResults.playerId);

      if(!matchUp){// if this is the first matchup with this opponent
        matchUp = { playerId: this.player!.playerId, avgDiff: 0, wins: 0, losses: 0, opponentId: opponentResults.playerId, opponentName: opponentResults.playerName, pointsFor: 0, pointsAgainst: 0 };
        allMatchups.push(matchUp);
      }

      if(this.calculateResult(results[i], this.player!.playerId) > 0){
        matchUp.wins++;
      }
      else
        matchUp.losses++;

      matchUp.pointsAgainst += opponentResults.score;
      matchUp.pointsFor += playerResults.score;
      matchUp.avgDiff = Math.round((matchUp.pointsFor - matchUp.pointsAgainst) / (matchUp.losses + matchUp.wins));
    }

    const matchupsWithStats = allMatchups.map((matchup) => {
      const totalMatches = matchup.wins + matchup.losses;
      const winPercentage = totalMatches === 0 ? 0 : (matchup.wins / totalMatches) * 100;
      return { ...matchup, totalMatches, winPercentage };
    });
  
    if(!this.tournamentId){
      // 2. Determine a Minimum Matchup Threshold
      const totalMatchupsPlayed = matchupsWithStats.reduce(
        (sum, m) => sum + m.totalMatches,
        0
      );
      const averageMatchups = totalMatchupsPlayed / matchupsWithStats.length;
      if(averageMatchups > 2)
        this.minMatchupsThreshold = Math.max(3, Math.round(averageMatchups / 2));
      else 
        this.minMatchupsThreshold = 0;
    
      // 3. Sort by Weighted Win Percentage for Best Matchups
      this.bestMatchups = matchupsWithStats
        .slice()
        .filter((m) => m.totalMatches >= this.minMatchupsThreshold)
        .sort((a, b) => {
          const weightedA = a.winPercentage * (a.totalMatches / this.minMatchupsThreshold);
          const weightedB = b.winPercentage * (b.totalMatches / this.minMatchupsThreshold);
          return weightedB - weightedA;
        })
        .slice(0, 10);
    
      // 4. Sort by Weighted Win Percentage for Worst Matchups
      this.worstMatchups = matchupsWithStats
        .slice()
        .filter((m) => m.totalMatches >= this.minMatchupsThreshold)
        .sort((a, b) => {
          const weightedA = a.winPercentage * (a.totalMatches / this.minMatchupsThreshold);
          const weightedB = b.winPercentage * (b.totalMatches / this.minMatchupsThreshold);
          return weightedA - weightedB; // Reverse for worst matchups
        })
        .slice(0, 10);
    }
    else {
      this.bestMatchups = matchupsWithStats.slice().sort((a,b) => { return b.wins - a.wins });
      this.worstMatchups = matchupsWithStats.slice().sort((a,b) => { return a.wins - b.wins });

    }
  }

  onTournamentChange(){
    this.tournamentId = (this.selectedTournamentId > 0) ? this.selectedTournamentId : null;
    this.loadPlayer(this.player!.playerId);
  }

  loggedIn():boolean{
    return this.authenticationService.isAdminLoggedIn();
  }
}
