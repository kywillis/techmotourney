import { Component, OnInit, ViewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { interval } from 'rxjs';
import { GameStatus } from 'src/app/enums';
import { IGameResult } from 'src/app/core/models/gameResult.model';
import { IGameResultPlayer } from 'src/app/core/models/gameResultPlayer.model';
import { IGameTeam } from 'src/app/core/models/gameTeam.model';
import { GameStationService } from 'src/app/core/services/game-station.service';
import { GameTeamsService } from 'src/app/core/services/gameTeams.service';
import { MessageComponent } from 'src/app/shared/components/message/message.component';

@Component({
  selector: 'app-game-station',
  templateUrl: './game-station.component.html',
  styleUrl: './game-station.component.less',
  standalone: false
})
export class GameStationComponent implements OnInit {
  private static readonly listRefreshIntervalMs = 30_000;

  @ViewChild('message') messageComponent!: MessageComponent;

  readonly GameStatus = GameStatus;

  /** Game id from route `/game-station/:gameResultId`, or null on the list URL. */
  routeGameId: number | null = null;

  /** Starts true so deep links to `/game-station/:id` wait for the first list fetch before binding. */
  loading = true;
  saving = false;
  loadError: string | null = null;

  tournamentName = '';
  waiting: IGameResult[] = [];
  inProgress: IGameResult[] = [];

  teams: IGameTeam[] = [];
  selectedGame: IGameResult | null = null;

  p1TeamId: number | null = null;
  p2TeamId: number | null = null;
  formError: string | null = null;

  constructor(
    private gameStationService: GameStationService,
    private gameTeamsService: GameTeamsService,
    private route: ActivatedRoute,
    private router: Router
  ) {
    this.route.paramMap.pipe(takeUntilDestroyed()).subscribe((pm) => {
      const prevId = this.routeGameId;
      const raw = pm.get('gameResultId');
      if (raw == null || raw === '') {
        this.routeGameId = null;
        this.selectedGame = null;
        this.formError = null;
        if (prevId != null) {
          this.loadGames();
        }
        return;
      }
      const id = +raw;
      if (!Number.isFinite(id) || id < 1) {
        void this.router.navigate(['/game-station'], { replaceUrl: true });
        return;
      }
      this.routeGameId = id;
      this.tryBindRouteGame();
    });

    interval(GameStationComponent.listRefreshIntervalMs)
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        if (this.routeGameId == null) {
          this.loadGames(true);
        }
      });
  }

  get showList(): boolean {
    return this.routeGameId == null;
  }

  get showDetail(): boolean {
    return this.selectedGame != null;
  }

  get showDetailLoading(): boolean {
    return this.routeGameId != null && this.loading;
  }

  /**
   * Resolves sprite index from API fields (`profilePic` or `ProfilePic`, string or number).
   * Without this, missing camelCase mapping made every player fall back to face 1.
   */
  private coerceProfilePicId(player: IGameResultPlayer): number {
    const row = player as unknown as Record<string, unknown>;
    const raw = row['profilePic'] ?? row['ProfilePic'];
    if (raw == null || raw === '') {
      return 1;
    }
    const n = typeof raw === 'string' ? parseInt(raw, 10) : Number(raw);
    if (!Number.isFinite(n) || n < 1) {
      return 1;
    }
    return Math.floor(n);
  }

  /** Ensures team id is a positive int; handles string JSON or PascalCase keys. */
  private coerceTeamId(raw: unknown): number {
    if (raw == null || raw === '') {
      return 0;
    }
    const n = typeof raw === 'string' ? parseInt(raw, 10) : Number(raw);
    return Number.isFinite(n) && n > 0 ? Math.floor(n) : 0;
  }

  private normalizePlayer(player: IGameResultPlayer): IGameResultPlayer {
    const row = player as unknown as Record<string, unknown>;
    const fromApi = this.coerceTeamId(row['gameTeamId'] ?? row['GameTeamId']) || this.coerceTeamId(player.gameTeamId);
    return {
      ...player,
      profilePic: this.coerceProfilePicId(player),
      gameTeamId: fromApi > 0 ? fromApi : player.gameTeamId ?? null
    };
  }

  private isGameInProgress(game: IGameResult): boolean {
    const status = game.status as unknown;
    return (
      status === GameStatus.InProgress ||
      status === 'InProgress' ||
      String(status) === 'InProgress'
    );
  }

  /**
   * Waiting: only explicit selections (no server defaults on load).
   * In-progress: uses dropdowns, with fallback to loaded `gameTeamId` if needed.
   */
  private effectiveTeamIds(): { t1: number; t2: number } | null {
    if (!this.selectedGame) {
      return null;
    }
    const inProgress = this.isGameInProgress(this.selectedGame);
    const t1 =
      this.coerceTeamId(this.p1TeamId) ||
      (inProgress ? this.coerceTeamId(this.selectedGame.player1.gameTeamId) : 0);
    const t2 =
      this.coerceTeamId(this.p2TeamId) ||
      (inProgress ? this.coerceTeamId(this.selectedGame.player2.gameTeamId) : 0);
    if (t1 <= 0 || t2 <= 0) {
      return null;
    }
    return { t1, t2 };
  }

  private normalizeGameResult(game: IGameResult): IGameResult {
    return {
      ...game,
      player1: this.normalizePlayer(game.player1),
      player2: this.normalizePlayer(game.player2)
    };
  }

  /** Classes for the `faces.png` sprite (same pattern as game-wagering-modal). */
  faceClass(player: IGameResultPlayer, variant: 'detail' | 'list' = 'detail'): string {
    const id = this.coerceProfilePicId(player);
    const size = variant === 'list' ? 'gs-list-face' : 'gs-station-face';
    return `player-face ${size} player-face-${id}`;
  }

  /** `helmets.png` sprite classes; empty when no team label. */
  helmetClasses(teamName: string | null | undefined): string {
    const t = (teamName ?? '').trim();
    if (!t) {
      return '';
    }
    return `helmet-small helmet-small-${t.toLowerCase()}`;
  }

  private static readonly easternTimeZone = 'America/New_York';

  /**
   * API stores `gameStartedAt` as UTC. JSON may omit "Z" (Unspecified from SQL); if so, treat as UTC
   * so `America/New_York` formatting is correct.
   */
  private static dateFromApiUtc(iso: string): Date {
    const s = String(iso).trim();
    if (/Z$/i.test(s) || /[+-]\d{2}:\d{2}$/.test(s)) {
      return new Date(s);
    }
    const normalized = s.includes('T') ? s : s.replace(' ', 'T');
    return new Date(`${normalized}Z`);
  }

  /** Wall-clock time in US Eastern when `gameStartedAt` is set (in-progress start at station). */
  startedAtLine(game: IGameResult): string | null {
    const raw = game.gameStartedAt;
    if (raw == null || String(raw).trim() === '') {
      return null;
    }
    const d = GameStationComponent.dateFromApiUtc(String(raw));
    if (Number.isNaN(d.getTime())) {
      return null;
    }
    const clock = d
      .toLocaleTimeString('en-US', {
        timeZone: GameStationComponent.easternTimeZone,
        hour: 'numeric',
        minute: '2-digit',
        hour12: true
      })
      .replace(/\s/g, '');
    return `started at: ${clock} ET`;
  }

  /** Full date/time in Eastern for native tooltip (`title`). */
  startedAtTooltip(game: IGameResult): string | null {
    const raw = game.gameStartedAt;
    if (raw == null || String(raw).trim() === '') {
      return null;
    }
    const d = GameStationComponent.dateFromApiUtc(String(raw));
    if (Number.isNaN(d.getTime())) {
      return null;
    }
    return d.toLocaleString('en-US', {
      timeZone: GameStationComponent.easternTimeZone,
      dateStyle: 'full',
      timeStyle: 'long'
    });
  }

  ngOnInit(): void {
    this.gameTeamsService.getAll().subscribe({
      next: (t) => (this.teams = t)
    });
    this.loadGames();
  }

  /**
   * @param silent When true (background refresh), keeps the list visible without the full-page loading state.
   */
  loadGames(silent = false): void {
    if (!silent) {
      this.loading = true;
    }
    this.loadError = null;
    this.gameStationService.getGames().subscribe({
      next: (r) => {
        this.tournamentName = r.tournamentName;
        this.waiting = r.waiting.map((g) => this.normalizeGameResult(g));
        this.inProgress = r.inProgress.map((g) => this.normalizeGameResult(g));
        if (!silent) {
          this.loading = false;
        }
        this.tryBindRouteGame();
      },
      error: (err) => {
        if (!silent) {
          this.loading = false;
        }
        const body = err?.error;
        this.loadError =
          body?.errorMessage ??
          body?.message ??
          'Could not load games. Is there an active tournament?';
      }
    });
  }

  private tryBindRouteGame(): void {
    if (this.routeGameId == null) {
      return;
    }
    if (this.loading) {
      return;
    }
    const all = [...this.waiting, ...this.inProgress];
    const game = all.find((g) => g.gameResultId === this.routeGameId);
    if (game) {
      this.bindGame(game);
    } else {
      void this.router.navigate(['/game-station'], { replaceUrl: true });
    }
  }

  private bindGame(game: IGameResult): void {
    this.selectedGame = game;
    if (this.isGameInProgress(game)) {
      const t1 = this.coerceTeamId(game.player1.gameTeamId);
      const t2 = this.coerceTeamId(game.player2.gameTeamId);
      this.p1TeamId = t1 > 0 ? t1 : null;
      this.p2TeamId = t2 > 0 ? t2 : null;
    } else {
      this.p1TeamId = null;
      this.p2TeamId = null;
    }
    this.formError = null;
  }

  openGame(game: IGameResult): void {
    void this.router.navigate(['/game-station', game.gameResultId]);
  }

  back(): void {
    void this.router.navigate(['/game-station']);
  }

  submit(): void {
    if (!this.selectedGame) {
      return;
    }
    const ids = this.effectiveTeamIds();
    if (!ids) {
      this.formError = 'Each player must select a team.';
      return;
    }
    const { t1, t2 } = ids;
    if (t1 === t2) {
      this.formError = 'Each player must use a different team.';
      return;
    }
    this.formError = null;
    const startGame = this.selectedGame.status === GameStatus.Waiting;
    this.saving = true;
    this.gameStationService
      .updateGame(this.selectedGame.gameResultId, {
        startGame,
        player1GameTeamId: t1,
        player2GameTeamId: t2
      })
      .subscribe({
        next: () => {
          this.saving = false;
          const msg = startGame ? 'Game started' : 'Teams updated';
          this.messageComponent.setMessage(msg, false);
          this.back();
        },
        error: (err) => {
          this.saving = false;
          const body = err?.error;
          const text = body?.errorMessage ?? body?.message ?? 'Could not save. Try again.';
          this.messageComponent.setMessage(text, true);
        }
      });
  }

  revertToWaiting(): void {
    if (!this.selectedGame) {
      return;
    }
    if (!this.isGameInProgress(this.selectedGame)) {
      return;
    }
    const ids = this.effectiveTeamIds();
    if (!ids) {
      this.formError = 'Each player must select a team.';
      return;
    }
    const { t1, t2 } = ids;
    if (t1 === t2) {
      this.formError = 'Each player must use a different team.';
      return;
    }
    this.formError = null;
    this.saving = true;
    this.gameStationService
      .updateGame(this.selectedGame.gameResultId, {
        startGame: false,
        revertToWaiting: true,
        player1GameTeamId: t1,
        player2GameTeamId: t2
      })
      .subscribe({
        next: () => {
          this.saving = false;
          this.messageComponent.setMessage('Game set back to waiting', false);
          this.back();
        },
        error: (err) => {
          this.saving = false;
          const body = err?.error;
          const text = body?.errorMessage ?? body?.message ?? 'Could not update status. Try again.';
          this.messageComponent.setMessage(text, true);
        }
      });
  }
}
