import { Component, OnInit, EventEmitter, Output, ViewChild, Input, OnChanges, SimpleChanges } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ResultsService } from 'src/app/core/services/results.service';
import { forkJoin, Observable } from 'rxjs';
import { PlayersService } from 'src/app/core/services/players.service';
import { GameTeamsService } from 'src/app/core/services/gameTeams.service';
import { IPlayer } from 'src/app/core/models/player.model';
import { IGameResult } from 'src/app/core/models/gameResult.model';
import { ISaveGameResultRequest } from 'src/app/core/models/request/saveGameResultRequest.model';
import { GameStatus, GameType } from 'src/app/enums';
import { MessageComponent } from '../message/message.component';
import { ISaveGameResultResponse } from 'src/app/core/models/save-game-result-response.model';
import { IGameResultPlayer } from 'src/app/core/models/gameResultPlayer.model';
import { getHttpErrorMessage } from 'src/app/core/utils/http-error.util';

@Component({
    selector: 'app-edit-game-result',
    templateUrl: './edit-game-result.component.html',
    styleUrls: ['./edit-game-result.component.less'],
    standalone: false
})
export class EditGameResultComponent implements OnInit {
  @Output() gameResultSaved: EventEmitter<void> = new EventEmitter();
  @Input() gameResults : IGameResult[] = [];
  @Input() tournamentId : number = 0;
  @ViewChild("message") messageComponent!: MessageComponent;
  
  gameResult? : IGameResult;
  gameResultForm: FormGroup;
  players: IPlayer[] = [];
  teams: any[] = []; // Placeholder for teams
  saving = false;
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
      status: [null, Validators.required],
      gameType: [null, Validators.required]
    }, { validators: [this.playersTeamsValidator()] });
  }

  ngOnInit() {
    forkJoin({
      teams: this.gameTeamsService.getAll()
    }).subscribe(({teams}) => {      
        teams.sort((a, b) => a.teamName.localeCompare(b.teamName));
        this.teams = teams;
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['gameResults'] && this.gameResults.length) {
      this.setGame(this.gameResults[0], this.players);
    }

    if (changes['tournamentId']) {
      this.loadPlayers();
    }
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
      gameType: formValues.gameType
    };

    const id = this.gameResult!.gameResultId;
    const call: Observable<ISaveGameResultResponse> =
      id && id > 0
        ? this.resultsService.updateResult(id, request)
        : this.resultsService.createResult(request);

    this.saving = true;
    call.subscribe({
      next: (res) => {
        this.saving = false;
        this.gameResultSaved.emit();
        let msg = 'Game saved.';
        const og =
          res.bracketReconciliation?.oddsGeneration?.attempted
            ? res.bracketReconciliation.oddsGeneration
            : res.oddsGeneration;
        if (og.attempted && !og.success) {
          msg += ' ' + (og.message || 'Odds generation failed.');
          this.messageComponent.setMessage(msg, true);
        } else {
          this.messageComponent.setMessage(msg);
        }
      },
      error: (errorResponse) => {
        this.saving = false;
        const detail = getHttpErrorMessage(errorResponse);
        this.messageComponent.setMessage(`There was an error saving the game: ${detail}`, true);
        console.error('Error saving game:', errorResponse);
      }
    });
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

  loadPlayers() {
    this.playersService.getPlayers(this.tournamentId).subscribe((players) => {
      players.sort((a, b) => a.fullName.localeCompare(b.fullName));
      this.players = players;
    });
  }

  setGame(gameResult: IGameResult, players: IPlayer[]): void {
    this.gameResult = gameResult;
    
    if(gameResult.player1){
      const team1 = this.teams.find(t => t.teamName === gameResult.player1.teamName);
      this.gameResult.player1.gameTeamId = team1 ? team1.gameTeamId : null;
    }
    
    if(gameResult.player2){
      const team2 = this.teams.find(t => t.teamName === gameResult.player2.teamName);
      this.gameResult.player2.gameTeamId = team2 ? team2.gameTeamId : null;
    }
    
    players.sort((a, b) =>
      a.fullName.localeCompare(b.fullName, undefined, { sensitivity: 'base' })
    );
    this.players = players;
    this.gameResultForm.reset();
  
    this.gameResultForm.patchValue({
      player1: this.getPlayerFormValues(this.gameResult.player1),
      player2: this.getPlayerFormValues(this.gameResult.player2),
      tournamentId: this.gameResult.tournamentId,
      status: this.gameResult.status,
      gameType: this.gameResult.gameType
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