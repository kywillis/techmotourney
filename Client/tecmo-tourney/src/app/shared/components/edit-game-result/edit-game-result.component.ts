import { Component, OnInit, EventEmitter, Output, ViewChild } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ResultsService } from 'src/app/core/services/results.service';
import { PlayersService } from 'src/app/core/services/players.service';
import { GameTeamsService } from 'src/app/core/services/gameTeams.service';
import { IPlayer } from 'src/app/core/models/player.model';
import { IGameResult } from 'src/app/core/models/gameResult.model';
import { ISaveGameResultRequest } from 'src/app/core/models/request/saveGameResultRequest.model';
//import { IGameResultStatsModel } from 'src/app/core/models/gameResultStats.model';
import { GameStatus, GameType } from 'src/app/enums';
import { forkJoin } from 'rxjs';
import { MessageComponent } from '../message/message.component';
import { IGameResultPlayer } from 'src/app/core/models/gameResultPlayer.model';

@Component({
  selector: 'app-edit-game-result',
  templateUrl: './edit-game-result.component.html',
  styleUrls: ['./edit-game-result.component.less']
})
export class EditGameResultComponent implements OnInit {
  @Output() gameResultSaved: EventEmitter<void> = new EventEmitter();
  @ViewChild("message") messageComponent!: MessageComponent;
  
  gameResult? : IGameResult;
  gameResultForm: FormGroup;
  players: IPlayer[] = [];
  teams: any[] = []; // Placeholder for teams
  statuses = Object.entries(GameStatus).map(([key, value]) => ({ value, display: key }));
  gameTypes = Object.entries(GameType).map(([key, value]) => ({ value, display: key }));

  constructor(
    private fb: FormBuilder,
    private resultsService: ResultsService,
    private playersService: PlayersService,
    private gameTeamsService: GameTeamsService
  ) {
    this.gameResultForm = this.fb.group({
      player1: this.fb.group({
        playerId: [null, Validators.required],
        teamId: [null, Validators.required],
        score: [null, Validators.required],
        passingYards: [null, Validators.required],
        rushingYards: [null, Validators.required],
      }),
      player2: this.fb.group({
        playerId: [null, Validators.required],
        teamId: [null, Validators.required],
        score: [null, Validators.required],
        passingYards: [null, Validators.required],
        rushingYards: [null, Validators.required],
      }),
      tournamentId: [null, Validators.required],
      bracketGameId: [null, Validators.required],
      status: [null, Validators.required],
      gameType: [null, Validators.required]
    }, { validators: [this.playersTeamsValidator()] });
  }

  ngOnInit() {
    this.gameTeamsService.getAll().subscribe({
      next: (teams) => {
        this.teams = teams;
      },
      error: (error) => {
        console.error('Error loading teams', error);
      }
    });
  }

  playersTeamsValidator() {
    return (group: FormGroup) => {
      const player1Id = group.get('player1.playerId')?.value;
      const player2Id = group.get('player2.playerId')?.value;
      const team1Id = group.get('player1.teamId')?.value;
      const team2Id = group.get('player2.teamId')?.value;

      if (player1Id && player2Id && player1Id === player2Id) {
        group.get('player2.playerId')?.setErrors({ samePlayer: true });
      }

      if (team1Id && team2Id && team1Id === team2Id) {
        group.get('player2.teamId')?.setErrors({ sameTeam: true });
      }
    };
  }

  onSubmit() {
    if (this.gameResultForm.invalid) {
      this.gameResultForm.markAllAsTouched();
      return;
    }

    const formValues = this.gameResultForm.value;

    const request: ISaveGameResultRequest = {
      gameResultId: this.gameResult!.gameResultId, // Or set the ID if updating an existing game result
      player1: this.createPlayerStats(formValues.player1),
      player2: this.createPlayerStats(formValues.player2),
      tournamentId: formValues.tournamentId,
      status: formValues.status,
      gameType: formValues.gameType,
      bracketGameId: formValues.bracketGameId
    };

    this.resultsService.createResult(request).subscribe(
      result => {
        this.gameResultSaved.emit();
        this.messageComponent.setMessage('game saved')
      },
      error => {
        this.messageComponent.setMessage('error saving game')
      }
    );
  }

  createPlayerStats(playerFormGroup: any): IGameResultPlayer {
    return {
      playerId: playerFormGroup.playerId,
      playerName: '',
      gameTeamId: playerFormGroup.teamId,
      teamName: '',
      score: playerFormGroup.score,
      passingYards: playerFormGroup.passingYards,
      rushingYards: playerFormGroup.rushingYards
    };
  }

  getPlayerNameById(playerId: number): string {
    const player = this.players.find(p => p.playerId === playerId);
    return player ? player.fullName : '';
  }

  getTeamNameById(teamId: number): string {
    const team = this.teams.find(t => t.id === teamId);
    return team ? team.name : '';
  }

  setGame(gameResult: IGameResult, players: IPlayer[]) : void{
    this.gameResult = gameResult;
    players.sort((a, b) => a.fullName.localeCompare(b.fullName, undefined, { sensitivity: 'base' }));
    this.players = players;
    this.gameResultForm.reset();

  this.gameResultForm.patchValue({
    player1: this.getPlayerFormValues(gameResult.player1),
    player2: this.getPlayerFormValues(gameResult.player2),
    tournamentId: gameResult.tournamentId,
    bracketGameId: gameResult.bracketGameId,
    status: gameResult.status,
    gameType: gameResult.gameType
  });
  }

  get player1Controls() {
    return (this.gameResultForm.get('player1') as FormGroup).controls;
  }

  get player2Controls() {
    return (this.gameResultForm.get('player2') as FormGroup).controls;
  }

  private getPlayerFormValues(player: IGameResultPlayer | null): any {
    if (player) {
      return {
        playerId: player.playerId ?? '',
        teamId: player.gameTeamId && player.gameTeamId > 0 ? player.gameTeamId : '',
        score: player.score ?? '',
        passingYards: player.passingYards ?? '',
        rushingYards: player.rushingYards ?? ''
      };
    } else {
      return {
        playerId: '',
        teamId: '',
        score: '',
        passingYards: '',
        rushingYards: ''
      };
    }
  }
  
}