import { Component, EventEmitter, Input, OnInit, Output, SimpleChanges } from '@angular/core';
import { IGameResult } from 'src/app/core/models/gameResult.model';
import { IPlayerStat } from 'src/app/core/models/playerStat';
import { IPlayer } from 'src/app/core/models/player.model';
import { StatType } from 'src/app/enums';
import { IGameResultPlayer } from 'src/app/core/models/gameResultPlayer.model';

@Component({
    selector: 'app-display-stats',
    templateUrl: './display-stats.component.html',
    styleUrls: ['./display-stats.component.less'],
    standalone: false
})
export class DisplayStatsComponent implements OnInit{
 @Input('games') games: IGameResult[] = [];
 @Input('players') players: IPlayer[] = [];
 @Input('statType') statType!: StatType;
 @Output() selectedGames = new EventEmitter<IGameResult[]>();

 gamesInAvg: number = 3;
 playerStats: IPlayerStat[] = [];
 ngOnInit(): void {

 }

   // Method called when the player is changed
   ngOnChanges(changes: SimpleChanges): void {
    console.log('change');
    if (changes['games'] || changes['statType']) {
      this.loadStats();
    }
  }

  loadStats(){
    switch(this.statType)
    {
      case StatType.HighestScore:
        this.calculateStat((games, player) => this.calculateHighestScore(games, player));
        break;
      case StatType.TotalOffensiveYards:
        this.calculateStat((games, player) => this.calculateTotalOffensiveYards(games, player));
        break;
      case StatType.TopPassingYards:
        this.calculateStat((games, player) => this.calculateHighestPassingYards(games, player));
        break;
      case StatType.TopRushingYards:
        this.calculateStat((games, player) => this.calculateHighestRushingYards(games, player));
        break;
      case StatType.FewestPointsAllowed:
        this.calculateStat((games, player) => this.calculateFewestPointsAllowed(games, player));
        break;
    }
  }
  
  calculateStat(fn: (games: IGameResult[], player: IPlayer)=> IPlayerStat){    
    this.playerStats = [];

    for (let i = 0; i < this.players.length; i++) {
      const playerGames = this.games
                          .filter((g) => { return g.player1.playerId == this.players[i].playerId || g.player2.playerId == this.players[i].playerId});
      const result = fn(playerGames, this.players[i]);
      this.playerStats.push(result);
    }

    this.playerStats = this.playerStats.sort((a, b) => {
      if (b.statValue !== a.statValue) {
        return b.statValue - a.statValue;  // Normal descending order
      }
      return 0;  // Keep equal values in their current order
    });

    if(this.statType == StatType.FewestPointsAllowed)
      this.playerStats = this.playerStats.reverse();

    const target = this.playerStats[0].statValue;
    for (let i = 0; i < this.playerStats.length; i++) {
      if(i == 0)
        continue;
      
      if(this.statType == StatType.FewestPointsAllowed)
        this.playerStats[i].neededToPass = this.getRequiredAllowedValue(target, this.playerStats[i].valuesInAvg, this.gamesInAvg);
      else 
        this.playerStats[i].neededToPass = this.getRequiredValue(target, this.playerStats[i].valuesInAvg, this.gamesInAvg);

      if(this.playerStats[i].neededToPass < 0)
        this.playerStats[i].neededToPass = 0;
    }
  }

  calculateHighestPassingYards(games: IGameResult[], player: IPlayer): IPlayerStat{      
    let statValue = 0;
    let valuesInAvg = [];

    if(games.length > 0){
      games = games.sort((g1, g2) => (this.getPlayerResults(g1, player.playerId).passingYards - (this.getPlayerResults(g2, player.playerId).passingYards))).reverse();;
      if(games.length > this.gamesInAvg)
      {
        games = games.slice(0,this.gamesInAvg);
      }

      for (let i = 0; i < games.length; i++) {
        const temp = this.getPlayerResults(games[i], player.playerId).passingYards;
        statValue += temp;
        valuesInAvg.push(temp);
      }
      statValue = statValue / games.length;
    }      

    return {
      playerName: player.fullName,
      playerId: player.playerId,
      statType: StatType.TotalOffensiveYards,
      statValue: Math.round(statValue),
      neededToPass: 0,
      valuesInAvg: valuesInAvg,
      games: games
    } as IPlayerStat
  }

  calculateHighestRushingYards(games: IGameResult[], player: IPlayer): IPlayerStat{      
    let statValue = 0;
    let valuesInAvg = [];

    if(games.length > 0){
      games = games.sort((g1, g2) => (this.getPlayerResults(g1, player.playerId).rushingYards - (this.getPlayerResults(g2, player.playerId).rushingYards))).reverse();;
      if(games.length > this.gamesInAvg)
      {
        games = games.slice(0,this.gamesInAvg);
      }

      for (let i = 0; i < games.length; i++) {
        const temp = this.getPlayerResults(games[i], player.playerId).rushingYards;
        statValue += temp;
        valuesInAvg.push(temp);
      }
      statValue = statValue / games.length;
    }      

    return {
      playerName: player.fullName,
      playerId: player.playerId,
      statType: StatType.TotalOffensiveYards,
      statValue: Math.round(statValue),
      neededToPass: 0,
      valuesInAvg: valuesInAvg,
      games: games
    } as IPlayerStat
  }

  calculateHighestScore(games: IGameResult[], player: IPlayer): IPlayerStat{      
    let statValue = 0;
    let valuesInAvg = [];

    if(games.length > 0){
      games = games.sort((g1, g2) => (this.getPlayerResults(g1, player.playerId).score - (this.getPlayerResults(g2, player.playerId).score))).reverse();
      if(games.length > this.gamesInAvg)
      {
        games = games.slice(0,this.gamesInAvg);
      }

      for (let i = 0; i < games.length; i++) {
        const temp = this.getPlayerResults(games[i], player.playerId).score;
        statValue += temp;
        valuesInAvg.push(temp);
      }
      statValue = statValue / games.length;
    }      

    return {
      playerName: player.fullName,
      playerId: player.playerId,
      statType: StatType.TotalOffensiveYards,
      statValue: Math.round(statValue),
      neededToPass: 0,
      valuesInAvg: valuesInAvg,
      games: games
    } as IPlayerStat
  }

  calculateTotalOffensiveYards(games: IGameResult[], player: IPlayer): IPlayerStat{      
      let statValue = 0;
      let valuesInAvg = [];

      if(games.length > 0){
        games = games.sort((g1, g2) => (this.getPlayerResults(g1, player.playerId).passingYards + this.getPlayerResults(g1, player.playerId).rushingYards) - (this.getPlayerResults(g2, player.playerId).passingYards + this.getPlayerResults(g2, player.playerId).rushingYards)).reverse();
        if(games.length > this.gamesInAvg)
        {
          games = games.slice(0,this.gamesInAvg);
        }

        for (let i = 0; i < games.length; i++) {
          const temp = this.getPlayerResults(games[i], player.playerId).passingYards + this.getPlayerResults(games[i], player.playerId).rushingYards;
          statValue += temp;
          valuesInAvg.push(temp);
        }
        statValue = statValue / games.length;
      }      

      return {
        playerName: player.fullName,
        playerId: player.playerId,
        statType: StatType.TotalOffensiveYards,
        statValue: Math.round(statValue),
        neededToPass: 0,
        valuesInAvg: valuesInAvg,
        games: games
      } as IPlayerStat
  }

  calculateFewestPointsAllowed(games: IGameResult[], player: IPlayer): IPlayerStat{      
    let statValue = 0;
    let valuesInAvg = [];

    if(games.length > 0){
      games = games.sort((g1, g2) => this.getPlayerResults(g2, player.playerId, true).score - this.getPlayerResults(g1, player.playerId, true).score).reverse();
      if(games.length > this.gamesInAvg)
      {
        games = games.slice(0,this.gamesInAvg);
      }

      for (let i = 0; i < games.length; i++) {
        const temp = this.getPlayerResults(games[i], player.playerId, true).score;
        statValue += temp;
        valuesInAvg.push(temp);
      }
      statValue = statValue / games.length;
    }      

    return {
      playerName: player.fullName,
      playerId: player.playerId,
      statType: StatType.TotalOffensiveYards,
      statValue: Math.round(statValue),
      neededToPass: 0,
      valuesInAvg: valuesInAvg,
      games: games
    } as IPlayerStat
}

  getPlayerResults(game: IGameResult, playerId: number, opponent: boolean = false) : IGameResultPlayer
  {
    if(game.player1.playerId == playerId)
    {
      if(opponent)
        return game.player2;
      else 
        return game.player1;
    }
    else
    {
      if(opponent)
        return game.player1;
      else
        return game.player2;
    }
  }
  
  getRequiredValue(
    targetAvg: number,
    mArr: number[],
    topCount: number = 3
  ): number {
    const count = mArr.length;
    const newCount = count + 1;
  
    let required: number;
  
    // If even after adding y we still have fewer than topCount numbers,
    // average over all available numbers.
    if (newCount <= topCount) {
      const sumM = mArr.reduce((acc, val) => acc + val, 0);
      required = targetAvg * newCount - sumM + 1;
    } else {
      // Otherwise, the new array has more than topCount numbers.
      // We assume y will be high enough to be among the top topCount values.
      // So, from the current mArr, take its top topCount numbers.
      const sortedM = mArr.slice().sort((a, b) => b - a);
      const topM = sortedM.slice(0, topCount);
      const sumTop = topM.reduce((acc, val) => acc + val, 0);
      const smallestTop = topM[topCount - 1];
      // After adding y, if y is high enough, it will replace the smallest of these top numbers.
      // New sum = (sumTop - smallestTop + y). We need:
      //   (sumTop - smallestTop + y) / topCount > targetAvg
      // Solve: y > targetAvg * topCount - (sumTop - smallestTop)
      required = targetAvg * topCount - (sumTop - smallestTop) + 1;
    }
  
    return required;
  }

  getRequiredAllowedValue(
    targetAvg: number,
    mArr: number[],
    topCount: number = 3
  ): number {
    const count = mArr.length;
    const total = mArr.reduce((acc, val) => acc + val, 0);
  
    // If the player has played at least one game and is already at target,
    // no extra (allowed) points are needed.
    if (count > 0 && total / count === targetAvg) {
      return 0;
    }
  
    const newCount = count + 1;
    let required: number;
  
    if (newCount <= topCount) {
      // With fewer than topCount games, the new average is over all games.
      // For a new game value y, we require:
      //    (total + y) / newCount < targetAvg
      // Since these are integers and we want a strict inequality,
      // the maximum allowed y that still beats targetAvg is:
      //    y = targetAvg * newCount - total + 1
      required = targetAvg * newCount - total + 1;
    } else {
      // With more than topCount games, only the best (i.e. lowest) topCount games count.
      const sorted = mArr.slice().sort((a, b) => a - b);
      const topGames = sorted.slice(0, topCount);
      const sumTop = topGames.reduce((acc, val) => acc + val, 0);
      const worstOfBest = topGames[topCount - 1];
      // In this branch, if y is low enough to be among the topCount,
      // it will replace worstOfBest. Then we require:
      //    (sumTop - worstOfBest + y) / topCount < targetAvg
      // Solving for y gives:
      //    y = targetAvg * topCount - (sumTop - worstOfBest) + 1
      required = targetAvg * topCount - (sumTop - worstOfBest) + 1;
    }
  
    return required;
  }

  selectGames(games: IGameResult[]){
    this.selectedGames.emit(games);
  }
}
