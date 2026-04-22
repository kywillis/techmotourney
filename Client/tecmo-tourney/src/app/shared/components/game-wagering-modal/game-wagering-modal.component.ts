import { Component, Input, OnInit, Optional } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { IPublicWageringSnapshot } from 'src/app/core/models/public-wagering-snapshot.model';
import { IGameResult } from 'src/app/core/models/gameResult.model';
import { ResultsService } from 'src/app/core/services/results.service';
import { GameStatus } from 'src/app/enums';

@Component({
  selector: 'app-game-wagering-modal',
  templateUrl: './game-wagering-modal.component.html',
  styleUrl: './game-wagering-modal.component.less',
  standalone: false
})
export class GameWageringModalComponent implements OnInit {
  @Input() snapshot!: IPublicWageringSnapshot;
  @Input() excludeGameResultId!: number;
  /** When true, used inline (e.g. bracket modal tab) — no modal chrome or NgbActiveModal required. */
  @Input() embedded = false;

  h2hGames: IGameResult[] = [];
  h2hLoading = true;

  GameStatus = GameStatus;

  /** Full class list for sprite face (avoids ngClass + static class merge issues). */
  get player1FaceClass(): string {
    return this.faceClassList(this.snapshot, 'player1ProfilePic', 'Player1ProfilePic');
  }

  get player2FaceClass(): string {
    return this.faceClassList(this.snapshot, 'player2ProfilePic', 'Player2ProfilePic');
  }

  private faceClassList(
    snap: IPublicWageringSnapshot,
    camelKey: 'player1ProfilePic' | 'player2ProfilePic',
    pascalKey: string
  ): string {
    const row = snap as unknown as Record<string, unknown>;
    const raw = row[camelKey] ?? row[pascalKey];
    const id = this.normalizeProfilePicId(raw);
    return `player-face matchup-face player-face-${id}`;
  }

  private normalizeProfilePicId(raw: unknown): number {
    const n = typeof raw === 'string' ? parseInt(raw, 10) : Number(raw);
    if (!Number.isFinite(n) || n < 1) {
      return 1;
    }
    return Math.floor(n);
  }

  constructor(
    @Optional() public activeModal: NgbActiveModal | null,
    private resultsService: ResultsService
  ) {}

  dismissChrome(): void {
    this.activeModal?.dismiss();
  }

  closeChrome(): void {
    this.activeModal?.close();
  }

  ngOnInit(): void {
    this.resultsService
      .searchResulsts({
        tournamentId: null,
        player1ID: this.snapshot.player1Id,
        player2ID: this.snapshot.player2Id,
        matchupLocation: null
      })
      .subscribe({
        next: (games) => {
          this.h2hGames = games
            .filter(
              (g) =>
                g.status === GameStatus.Completed &&
                g.gameResultId !== this.excludeGameResultId
            )
            .sort(
              (a, b) =>
                new Date(b.date).getTime() - new Date(a.date).getTime()
            );
          this.h2hLoading = false;
        },
        error: () => {
          this.h2hLoading = false;
        }
      });
  }

  formatAmerican(v: number | null | undefined): string {
    if (v == null || v === 0) {
      return '—';
    }
    const inv = v.toFixed(1);
    return v > 0 ? `+${inv}` : inv;
  }

  spreadDisplayForPlayer(playerId: number): string {
    const o = this.snapshot.odds;
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

  formatMoney(n: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      maximumFractionDigits: 0
    }).format(n);
  }
}
