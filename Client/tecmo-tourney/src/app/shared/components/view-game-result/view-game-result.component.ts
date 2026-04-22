import { Router } from '@angular/router';
import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  OnInit,
  Output,
  SimpleChanges
} from '@angular/core';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { IGameResult } from 'src/app/core/models/gameResult.model';
import { IPublicWageringSnapshot } from 'src/app/core/models/public-wagering-snapshot.model';
import { GameStatus } from 'src/app/enums';
import { GoogleAuthService } from 'src/app/core/services/google-auth.service';
import { ResultsService } from 'src/app/core/services/results.service';
import { GameWageringModalComponent } from '../game-wagering-modal/game-wagering-modal.component';

@Component({
  selector: 'app-view-game-result',
  templateUrl: './view-game-result.component.html',
  styleUrl: './view-game-result.component.less',
  standalone: false
})
export class ViewGameResultComponent implements OnInit, OnChanges {
  @Input() gameResult?: IGameResult;
  @Input() showControls: boolean = true;
  @Input() showStatus: boolean = true;
  @Input() showDatePlayed: boolean = false;
  @Input() playerIdSpotLight: number | null = null;
  /** When true, loads public lines for this game (if odds exist). */
  @Input() showWageringBand: boolean = false;
  /**
   * When true with showWageringBand, uses parentWageringSnapshot instead of GET per game.
   * Parent should load tournament batch and pass the snapshot for this gameResultId (or null).
   */
  @Input() useParentWageringData = false;
  /** From parent batch map; may be undefined if this game has no odds row. */
  @Input() parentWageringSnapshot: IPublicWageringSnapshot | null | undefined = null;

  @Output() editGame = new EventEmitter<IGameResult>();
  @Output() deleteGame = new EventEmitter<IGameResult>();

  GameStatus = GameStatus;
  wageringSnapshot: IPublicWageringSnapshot | null = null;

  constructor(
    private router: Router,
    private googleAuth: GoogleAuthService,
    private resultsService: ResultsService,
    private modal: NgbModal
  ) {}

  ngOnInit(): void {
    this.tryLoadWagering();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (
      changes['gameResult'] ||
      changes['showWageringBand'] ||
      changes['useParentWageringData'] ||
      changes['parentWageringSnapshot']
    ) {
      this.tryLoadWagering();
    }
  }

  private tryLoadWagering(): void {
    this.wageringSnapshot = null;
    if (!this.showWageringBand || !this.gameResult?.gameResultId) {
      return;
    }
    if (this.useParentWageringData) {
      this.wageringSnapshot = this.parentWageringSnapshot ?? null;
      return;
    }
    this.resultsService
      .getWageringSnapshot(this.gameResult.gameResultId)
      .subscribe((s) => {
        this.wageringSnapshot = s;
      });
  }

  get wageringHasSummary(): boolean {
    return !!this.wageringSnapshot?.odds.summary?.trim();
  }

  openWageringModal(): void {
    if (!this.wageringSnapshot || !this.gameResult) {
      return;
    }
    const ref = this.modal.open(GameWageringModalComponent, {
      size: 'lg',
      scrollable: true
    });
    ref.componentInstance.snapshot = this.wageringSnapshot;
    ref.componentInstance.excludeGameResultId = this.gameResult.gameResultId;
  }

  formatAmerican(v: number | null | undefined): string {
    if (v == null || v === 0) {
      return '—';
    }
    const inv = v.toFixed(1);
    return v > 0 ? `+${inv}` : inv;
  }

  spreadDisplayForPlayer(playerId: number): string {
    if (!this.wageringSnapshot) {
      return '';
    }
    const o = this.wageringSnapshot.odds;
    const mag = Math.abs(o.spread);
    const s = mag.toFixed(1);
    if (o.favoredPlayerId == null) {
      return s;
    }
    if (o.favoredPlayerId === playerId) {
      return `-${s}`;
    }
    return `+${s}`;
  }

  editGameResult(gameResult: IGameResult) {
    this.editGame.emit(gameResult);
  }

  deleteGameResult(gameResult: IGameResult) {
    this.deleteGame.emit(gameResult);
  }

  showPlayer(playerId: number) {
    this.router.navigate(['/players', playerId]);
  }

  spotlightWon(gameResult: IGameResult, score: number): boolean {
    if (
      this.playerIdSpotLight == gameResult.player1.playerId &&
      gameResult.player1.score == score &&
      gameResult.player1.score > gameResult.player2.score
    ) {
      return true;
    }

    if (
      this.playerIdSpotLight == gameResult.player2.playerId &&
      gameResult.player2.score == score &&
      gameResult.player2.score > gameResult.player1.score
    ) {
      return true;
    }

    return false;
  }

  statusDisplayLabel(): string {
    switch (this.gameResult?.status) {
      case GameStatus.Waiting:
        return 'Waiting';
      case GameStatus.InProgress:
        return 'In progress';
      case GameStatus.Completed:
        return 'Completed';
      default:
        return String(this.gameResult?.status ?? '');
    }
  }

  getSeedingExemptPlayerName(): string {
    if (!this.gameResult?.seedingExemptPlayerId) {
      return '';
    }
    if (this.gameResult.seedingExemptPlayerId === this.gameResult.player1.playerId) {
      return this.gameResult.player1.playerName;
    }
    if (this.gameResult.seedingExemptPlayerId === this.gameResult.player2.playerId) {
      return this.gameResult.player2.playerName;
    }
    return '';
  }

  loggedIn(): boolean {
    return this.googleAuth.isAdminLoggedIn();
  }
}
