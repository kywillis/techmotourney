import { Component, OnDestroy, OnInit, ViewChild  } from '@angular/core';
import { TournamentsService } from 'src/app/core/services/tournaments.service';
import { PlayersService } from 'src/app/core/services/players.service';
import { ResultsService } from 'src/app/core/services/results.service';
import { ActivatedRoute, Router } from '@angular/router';
import { ITournament } from 'src/app/core/models/tournament.model';
import { IPlayer } from 'src/app/core/models/player.model';
import { forkJoin, from, interval, Subscription } from 'rxjs';
import { concatMap } from 'rxjs/operators';
import { IGameResult } from 'src/app/core/models/gameResult.model';
import { IPublicWageringSnapshot } from 'src/app/core/models/public-wagering-snapshot.model';
import { StatType, GameType, TournamentStatus, GameStatus } from 'src/app/enums';
import { DisplayStatsComponent } from '../display-stats/display-stats.component';
import {
  ModalComponent,
  ModalTitleSegment
} from 'src/app/shared/components/modal/modal.component';
import { EditGameResultComponent } from 'src/app/shared/components/edit-game-result/edit-game-result.component';
import { DeleteGameResultComponent } from 'src/app/shared/components/delete-game-result/delete-game-result.component';
import { ViewGameResultComponent } from 'src/app/shared/components/view-game-result/view-game-result.component';
import { ITournamentStanding } from 'src/app/core/models/tournamentStandingModel';
import { IChangeTournamentStatusRequest } from 'src/app/core/models/request/changeTournamentStatusRequest.model';
import { MatTabGroup } from '@angular/material/tabs';
import { IGameSearchParameters } from 'src/app/core/models/gameSearchParameters';
import { DatePipe } from '@angular/common';
import { GoogleAuthService } from 'src/app/core/services/google-auth.service';
import { NotificationLogService } from 'src/app/core/services/notification-log.service';
import { TournamentAdminNavService } from 'src/app/core/services/tournament-admin-nav.service';
import { ISaveGameResultRequest } from 'src/app/core/models/request/saveGameResultRequest.model';
import { getHttpErrorMessage } from 'src/app/core/utils/http-error.util';
import { GameTeamsService } from 'src/app/core/services/gameTeams.service';
import { IGameTeam } from 'src/app/core/models/gameTeam.model';
import { IResetTournamentRequest } from 'src/app/core/models/request/resetTournamentRequest.model';
import { IBracketOddsLine, IPointSpread } from 'src/app/core/models/pointSpread.model';
import {
  tournamentHasBracketImage,
  tournamentUsesLegacyJqueryBracket
} from '../../bracket/bracket-legacy.util';
import { ResolvedMatch } from '../../bracket/double-elim-bracket.types';
declare const $: any;

@Component({
    selector: 'app-view-tournament',
    templateUrl: './view-tournament.component.html',
    styleUrls: ['./view-tournament.component.less'],
    standalone: false
})
export class ViewTournamentComponent implements OnInit, OnDestroy  {
  @ViewChild('resetModal') resetModal!: ModalComponent;
  @ViewChild('restartPhaseModal') restartPhaseModal!: ModalComponent;
  @ViewChild('fakePrelimModal') fakePrelimModal!: ModalComponent;
  @ViewChild('deleteGameResultModal') deleteGameResultModal!: ModalComponent;
  @ViewChild('editGameResultModal') editGameResultModal!: ModalComponent;
  @ViewChild('viewGameResultModal') viewGameResultModal!: ModalComponent;
  @ViewChild('viewGameResultsModal') viewGameResultsModal!: ModalComponent;
  @ViewChild('bracketNoMatchModal') bracketNoMatchModal!: ModalComponent;
  @ViewChild('bracketGameModal') bracketGameModal!: ModalComponent;
  @ViewChild('bracketGameEdit') bracketGameEdit?: EditGameResultComponent;
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
  private routeParamsSub?: Subscription;
  private adminLoggedSubscription?: Subscription;
  private tournamentNavSub = new Subscription();
  private lastLoadedTournamentId: number | null = null;
  private pendingTabSlug: string | null = null;
  private syncingTabFromRoute = false;
  /** URL segments for mat-tab indices 0..2 */
  private readonly tabSlugs = ['preliminaries', 'bracket', 'stats'] as const;
  selectedStatType : StatType = StatType.HighestScore;
  tournamentCompleted: boolean = false;
  resetError: string = '';
  restartPhaseError: string = '';
  /** Incomplete prelim games when opening the fake-results modal (snapshot). */
  incompletePrelimGamesForFake: IGameResult[] = [];
  fakePrelimError = '';
  fakePrelimSaving = false;
  showResetControls: boolean = false;
  loading = false;
  /** Inline spinners next to primary action buttons (not full-page `loading`). */
  startingPrelims = false;
  startingTournament = false;
  resettingTournament = false;
  restartingPhase = false;
  /** Populated with loadGames(); used by prelim list to avoid per-game wagering HTTP. */
  wageringSnapshotsByGameId: Record<number, IPublicWageringSnapshot> = {};
  /** Tournament bracket games: lines keyed by gameResultId for the double-elim viewer. */
  oddsByGameResultId: Record<number, IBracketOddsLine> = {};

  bracketNoMatchModalTitle = 'Bracket game';
  bracketGameModalTitleSegments: ModalTitleSegment[] | null = null;
  bracketGameModalAdmin = false;
  /** Game shown in the bracket click modal (preview / admin edit). */
  bracketModalGame: IGameResult | null = null;

  recalculatingBracket = false;

  /** Same catalog as edit-game (`GameTeamsService.getAll()`); used for fake prelim team picks. */
  gameTeams: IGameTeam[] = [];

  constructor(private tournamentService: TournamentsService, 
    private playersService: PlayersService, 
    private resultService: ResultsService, 
    private route: ActivatedRoute,
    private router: Router,
    private datePipe: DatePipe, 
    private googleAuth: GoogleAuthService,
    private notificationLog: NotificationLogService,
    private tournamentAdminNav: TournamentAdminNavService,
    private gameTeamsService: GameTeamsService) { }

  ngOnInit(): void {
    this.gameTeamsService.getAll().subscribe((teams) => {
      teams.sort((a, b) => a.teamName.localeCompare(b.teamName));
      this.gameTeams = teams;
    });

    this.routeParamsSub = this.route.paramMap.subscribe((pm) => {
      const id = pm.get('id');
      if (!id) return;
      const tid = +id;
      if (!Number.isFinite(tid) || tid < 1) {
        void this.router.navigate(['/tournaments'], { replaceUrl: true });
        return;
      }
      this.pendingTabSlug = pm.get('tab');
      if (this.lastLoadedTournamentId !== tid) {
        this.lastLoadedTournamentId = tid;
        this.loadTournamentData(tid);
      } else {
        this.applyTabFromUrlAfterLoad();
      }
    });

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
      if (this.iframeBracketActive()) {
        this.sendBracketMessage('setAdmin', val);
      }
    });

    this.tournamentNavSub.add(
      this.tournamentAdminNav.resetEntireRequested$.subscribe(() => {
        if (this.showResetEntireTournamentActions()) this.showReset();
      })
    );
    this.tournamentNavSub.add(
      this.tournamentAdminNav.restartPhaseRequested$.subscribe(() => {
        if (this.showRestartTournamentPhaseButton()) this.showRestartPhaseModal();
      })
    );
    this.tournamentNavSub.add(
      this.tournamentAdminNav.fillFakePrelimRequested$.subscribe(() => {
        if (this.showFillFakePrelimResults()) this.showFillFakePrelimModal();
      })
    );
    this.tournamentNavSub.add(
      this.tournamentAdminNav.recalculateBracketRequested$.subscribe(() => {
        if (this.showRecalculateBracketButton()) this.recalculateBracket();
      })
    );

    interval(30 * 1000).subscribe(() => { //get updates to games and standings every 30 seconds      
        this.loadGames();      
    });
  }

  ngOnDestroy(): void {
    this.routeParamsSub?.unsubscribe();
    this.adminLoggedSubscription?.unsubscribe();
    this.tournamentNavSub.unsubscribe();
    this.tournamentAdminNav.clearTournamentMenuFlags();
  }

  startPrelims(): void{
    this.startingPrelims = true;
    var statusRequest = {
      status: TournamentStatus.Preliminaries,
      tournamentId: this.tournament!.tournamentId
    } as IChangeTournamentStatusRequest;
    this.tournamentService.setStatus(statusRequest).subscribe({
      next: (res) => {
        this.tournament = res.tournament;
        const og = res.oddsGeneration;
        let text = 'Prelims started.';
        let level: 'info' | 'error' | 'success' = 'info';
        if (og.attempted) {
          if (og.success) {
            text += ' Odds generated.';
            level = 'success';
          } else {
            text += ` Odds: ${og.message || 'generation failed.'}.`;
            level = 'error';
          }
        }
        this.notificationLog.add({ level, text });
        this.refreshTournamentAdminMenuFlags();
        this.loadGames({ endPageLoad: true });
      },
      error: () => {
        this.startingPrelims = false;
        this.notificationLog.add({ level: 'error', text: 'Failed to start prelims.' });
      }
    });
  }

  loadTournamentData(tournamentId: number) {
    this.showResetControls = false;
    this.loading = true;

    forkJoin({
      tournament: this.tournamentService.getTournament(tournamentId),
      players: this.playersService.getPlayers(tournamentId)
    }).subscribe(({ tournament, players }) => {
      this.tournament = tournament;
      this.players = players;
      this.loading = false;

      if (this.tournament.status == TournamentStatus.Deleted || this.tournament.status == TournamentStatus.Waiting) {
        this.tournamentCompleted = false;
        this.refreshTournamentAdminMenuFlags();
        return;
      }

      this.loadGames();
      this.loadPointSpreads();

      this.tournamentCompleted = this.tournament.status == TournamentStatus.Completed;
      this.applyTabFromUrlAfterLoad();
      this.refreshTournamentAdminMenuFlags();
    });
  }

  /** Drives header hamburger visibility for Reset / Reset-to-prelims actions. */
  private refreshTournamentAdminMenuFlags(): void {
    if (!this.tournament) {
      this.tournamentAdminNav.setTournamentMenuFlags({
        resetEntire: false,
        restartPhase: false,
        fillFakePrelimResults: false,
        recalculateBracket: false
      });
      return;
    }
    this.tournamentAdminNav.setTournamentMenuFlags({
      resetEntire: this.showResetEntireTournamentActions(),
      restartPhase: this.showRestartTournamentPhaseButton(),
      fillFakePrelimResults: this.showFillFakePrelimResults(),
      recalculateBracket: this.showRecalculateBracketButton()
    });
  }

  private tabSlugToIndex(slug: string): number {
    return (this.tabSlugs as readonly string[]).indexOf(slug);
  }

  private tabIndexToSlug(index: number): string {
    return this.tabSlugs[index] ?? 'preliminaries';
  }

  private defaultTabSlugForTournament(): string {
    if (!this.tournament) return 'preliminaries';
    if (this.tournament.status === TournamentStatus.Preliminaries) return 'preliminaries';
    return 'bracket';
  }

  /**
   * Keeps mat-tab index in sync with route :tab; normalizes /tournaments/:id to a default tab.
   */
  private applyTabFromUrlAfterLoad(): void {
    if (!this.tournament || this.tournament.status === TournamentStatus.Waiting || this.tournament.status === TournamentStatus.Deleted) {
      return;
    }

    const tab = this.pendingTabSlug ?? this.route.snapshot.paramMap.get('tab');

    this.syncingTabFromRoute = true;
    try {
      if (!tab) {
        const slug = this.defaultTabSlugForTournament();
        this.router.navigate(['/tournaments', this.tournament.tournamentId, slug], { replaceUrl: true });
        this.selectedIndex = Math.max(0, this.tabSlugToIndex(slug));
        if (this.selectedIndex === 1 && this.iframeBracketActive()) {
          this.sendBracketMessage('initBracket', this.tournament.bracketData);
        }
        return;
      }

      const idx = this.tabSlugToIndex(tab);
      if (idx < 0) {
        this.router.navigate(['/tournaments', this.tournament.tournamentId, 'preliminaries'], { replaceUrl: true });
        this.selectedIndex = 0;
        return;
      }

      this.selectedIndex = idx;
      if (idx === 1 && this.iframeBracketActive()) {
        this.sendBracketMessage('initBracket', this.tournament.bracketData);
      }
    } finally {
      setTimeout(() => {
        this.syncingTabFromRoute = false;
      }, 0);
    }
  }

  /**
   * @param endPageLoad When true, clears `loading` after games/standings arrive (use after status changes that replace the whole view).
   * @param afterLoaded Runs after games/standings refresh succeeds or fails (e.g. navigate to a tab).
   */
  loadGames(options?: { endPageLoad?: boolean; afterLoaded?: () => void }) {
    const endPageLoad = options?.endPageLoad ?? false;
    const afterLoaded = options?.afterLoaded;
    forkJoin({
      games: this.resultService.getResultsByTournmanentId(this.tournament!.tournamentId),
      standings: this.tournamentService.getTournamentStandings(
        this.tournament!.tournamentId,
        this.tournament!.status
      ),
      wageringSnapshots: this.resultService.getWageringSnapshotsByTournament(this.tournament!.tournamentId),
      pointSpreads: this.resultService.getPointSpreads(this.tournament!.tournamentId)
    }).subscribe({
      next: ({ games, standings, wageringSnapshots, pointSpreads }) => {
        const prelim = games.filter((game) => game.gameType === GameType.Preliminary);
        this.prelimGames = this.sortPrelimGamesForDisplay(prelim);
        this.filteredPrelimGames = this.prelimGames;
        this.allGames = games;
        this.standings = standings;
        this.oddsByGameResultId = this.buildBracketOddsByGameId(pointSpreads, games);
        const byId: Record<number, IPublicWageringSnapshot> = {};
        for (const s of wageringSnapshots) {
          byId[s.gameResultId] = s;
        }
        this.wageringSnapshotsByGameId = byId;
        if (endPageLoad) {
          this.loading = false;
          this.startingPrelims = false;
          this.startingTournament = false;
        }
        afterLoaded?.();
      },
      error: () => {
        if (endPageLoad) {
          this.loading = false;
          this.startingPrelims = false;
          this.startingTournament = false;
        }
        afterLoaded?.();
      }
    });
  }

  loadPointSpreads() {
    this.resultService.getPointSpreads(this.tournament!.tournamentId).subscribe((results) => {
      if (results.length > 0 && this.iframeBracketActive()) {
        this.sendBracketMessage('pointSpreads', results);
      }
    });
  }

  private buildBracketOddsByGameId(spreads: IPointSpread[], games: IGameResult[]): Record<number, IBracketOddsLine> {
    const map: Record<number, IBracketOddsLine> = {};
    const normalizeFav = (v: number | null | undefined) => (v == null || v === 0 ? null : v);
    const toLine = (p: IPointSpread): IBracketOddsLine => ({
      spread: Number(p.spread),
      favoredPlayerId: normalizeFav(p.favoredPlayerId)
    });
    for (const p of spreads) {
      const gid = p.gameResultId;
      if (gid != null && gid > 0) {
        map[gid] = toLine(p);
      }
    }
    const tgames = games.filter((g) => g.gameType === GameType.Tournament);
    const spreadP1 = (s: IPointSpread) => s.player1Id ?? s.player1ID;
    const spreadP2 = (s: IPointSpread) => s.player2Id ?? s.player2ID;
    for (const g of tgames) {
      if (map[g.gameResultId] != null) {
        continue;
      }
      const p = spreads.find((s) => {
        const a = spreadP1(s);
        const b = spreadP2(s);
        return (
          (a === g.player1.playerId && b === g.player2.playerId) ||
          (a === g.player2.playerId && b === g.player1.playerId)
        );
      });
      if (p) {
        map[g.gameResultId] = toLine(p);
      }
    }
    return map;
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

  /** Double-elim bracket card click: no persisted game → message; else preview or admin tabs. */
  onBracketMatchActivate(payload: { match: ResolvedMatch; code: string }): void {
    const m = payload.match;
    const gid = m.gameResultId;
    this.bracketNoMatchModalTitle = payload.code ? payload.code : 'Bracket game';

    if (gid == null || gid < 1) {
      this.bracketNoMatchModal.open();
      return;
    }

    const game = this.allGames.find((g) => g.gameResultId === gid);
    if (!game) {
      this.bracketNoMatchModal.open();
      return;
    }

    this.bracketModalGame = game;
    this.bracketGameModalAdmin = this.googleAuth.isAdminLoggedIn();
    this.bracketGameModalTitleSegments = this.buildBracketGameModalTitleSegments(game);

    this.bracketGameModal.open();
  }

  /** Wagering snapshot for bracket modal Preview tab (may be missing). */
  get bracketModalPreviewSnapshot(): IPublicWageringSnapshot | undefined {
    if (!this.bracketModalGame) {
      return undefined;
    }
    return this.wageringSnapshotsByGameId[this.bracketModalGame.gameResultId];
  }

  private buildBracketGameModalTitleSegments(game: IGameResult): ModalTitleSegment[] {
    if (game.status === GameStatus.Waiting) {
      return [{ text: 'Waiting' }];
    }
    if (game.status === GameStatus.InProgress) {
      return [{ text: 'In progress' }];
    }
    if (game.status === GameStatus.Completed) {
      const d = this.datePipe.transform(game.date, 'h:mma M/d/yy') || '';
      return [
        { text: 'Completed', cssClass: 'modal-title-status-completed' },
        { text: ': ' + d }
      ];
    }
    const d = this.datePipe.transform(game.date, 'h:mma M/d/yy') || 'Game';
    return [{ text: d }];
  }

  /** Ensures the Edit tab form is bound when the admin selects it (lazy-friendly). */
  onBracketModalTabChange(index: number): void {
    if (index !== 1 || !this.bracketModalGame || !this.bracketGameModalAdmin) {
      return;
    }
    setTimeout(() => {
      this.bracketGameEdit?.setGame(this.bracketModalGame!, this.players);
    });
  }

  onBracketGameEditSaved(): void {
    this.bracketGameModal.close();
    this.bracketModalGame = null;
    this.loadGames();
  }

  onBracketGameModalClosed(): void {
    this.bracketModalGame = null;
    this.bracketGameModalTitleSegments = null;
  }

  onDeleteGame(gameResult: IGameResult) {
    this.deleteGameResult.setGame(gameResult);
    this.deleteGameResultModal.open();
  }

  startTournament() {
    const statusRequest: IChangeTournamentStatusRequest = {
      status: TournamentStatus.Tournament,
      tournamentId: this.tournament!.tournamentId,
      newGames: [],
      bracketData: {}
    };

    this.startingTournament = true;
    this.tournamentService.setStatus(statusRequest).subscribe({
      next: (res) => {
        this.tournament = res.tournament;
        this.refreshTournamentAdminMenuFlags();
        const og = res.oddsGeneration;
        if (og.attempted && og.success) {
          this.notificationLog.add({ level: 'success', text: 'Tournament started. Odds generated.' });
        } else if (og.attempted && !og.success) {
          this.notificationLog.add({
            level: 'error',
            text: `Tournament started. Odds: ${og.message || 'generation failed.'}`
          });
        } else {
          this.notificationLog.add({ level: 'info', text: 'Tournament started.' });
        }
        this.pendingTabSlug = 'bracket';
        this.loadGames({
          endPageLoad: true,
          afterLoaded: () => {
            this.selectedIndex = 1;
            void this.router.navigate(['/tournaments', this.tournament!.tournamentId, 'bracket']);
          }
        });
      },
      error: () => {
        this.startingTournament = false;
        this.notificationLog.add({ level: 'error', text: 'Failed to start tournament.' });
      }
    });
  }

  onTabChange(index: number): void {
    if (this.syncingTabFromRoute || !this.tournament) {
      return;
    }
    const slug = this.tabIndexToSlug(index);
    const cur = this.route.snapshot.paramMap.get('tab');
    if (cur === slug) {
      return;
    }
    this.router.navigate(['/tournaments', this.tournament.tournamentId, slug]);
  }

  filterPrelimGames(playerId: number){
    if(playerId < 1){
      this.filteredPlayerName = 'Prelim';
      this.filteredPrelimGames = this.prelimGames;
    }
    else {
      this.filteredPlayerName = this.players.filter(p => p.playerId == playerId)[0].fullName + ' ';
      const filtered = this.prelimGames.filter(g => g.player1.playerId == playerId || g.player2.playerId == playerId);
      this.filteredPrelimGames = this.sortPrelimGamesForDisplay(filtered);
    }
  }

  /** Waiting first, then In progress, then Completed (by game date descending). */
  private sortPrelimGamesForDisplay(games: IGameResult[]): IGameResult[] {
    const rank = (s: GameStatus): number => {
      switch (s) {
        case GameStatus.Waiting:
          return 0;
        case GameStatus.InProgress:
          return 1;
        case GameStatus.Completed:
          return 2;
        default:
          return 3;
      }
    };
    const time = (g: IGameResult): number => {
      const d = g.date;
      if (d == null) {
        return 0;
      }
      const t = new Date(d as unknown as string).getTime();
      return Number.isNaN(t) ? 0 : t;
    };
    return [...games].sort((a, b) => {
      const ra = rank(a.status);
      const rb = rank(b.status);
      if (ra !== rb) {
        return ra - rb;
      }
      if (a.status === GameStatus.Completed && b.status === GameStatus.Completed) {
        return time(b) - time(a);
      }
      if (a.status === GameStatus.Waiting && b.status === GameStatus.Waiting) {
        return time(a) - time(b);
      }
      if (a.status === GameStatus.InProgress && b.status === GameStatus.InProgress) {
        return time(b) - time(a);
      }
      return a.gameResultId - b.gameResultId;
    });
  }

  hasBracketImage(t: ITournament): boolean {
    return tournamentHasBracketImage(t);
  }

  usesLegacyJqueryBracket(t: ITournament): boolean {
    return tournamentUsesLegacyJqueryBracket(t);
  }

  showRecalculateBracketButton(): boolean {
    return !!(
      this.tournament &&
      this.tournament.status === TournamentStatus.Tournament &&
      !this.hasBracketImage(this.tournament) &&
      !this.usesLegacyJqueryBracket(this.tournament) &&
      this.googleAuth.isAdminLoggedIn()
    );
  }

  recalculateBracket(): void {
    if (!this.tournament || this.recalculatingBracket) {
      return;
    }
    this.recalculatingBracket = true;
    this.tournamentService.recalculateBracket(this.tournament.tournamentId).subscribe({
      next: () => {
        this.recalculatingBracket = false;
        this.loadGames();
        this.loadPointSpreads();
      },
      error: () => {
        this.recalculatingBracket = false;
      }
    });
  }

  /** Legacy iframe + jQuery bracket (requires populated bracketData, no static image). */
  iframeBracketActive(): boolean {
    return !!(
      this.tournament &&
      !tournamentHasBracketImage(this.tournament) &&
      tournamentUsesLegacyJqueryBracket(this.tournament)
    );
  }

  sendBracketMessage(type: string, data: any) {
    if (!this.iframeBracketActive()) {
      return;
    }
    console.log('new bracket update');
    console.log(data);
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

  /** Bottom bar: full reset for prelims / active bracket / completed. */
  showResetEntireTournamentActions(): boolean {
    if (!this.tournament) return false;
    const s = this.tournament.status;
    return (
      s === TournamentStatus.Preliminaries ||
      s === TournamentStatus.Tournament ||
      s === TournamentStatus.Completed
    );
  }

  /** Bottom bar: bracket-phase reset only when a bracket has been started or event finished. */
  showRestartTournamentPhaseButton(): boolean {
    if (!this.tournament) return false;
    const s = this.tournament.status;
    return s === TournamentStatus.Tournament || s === TournamentStatus.Completed;
  }

  /** Debug: fill incomplete prelim games — only while tournament is in preliminaries phase. */
  showFillFakePrelimResults(): boolean {
    if (!this.tournament) return false;
    return this.tournament.status === TournamentStatus.Preliminaries;
  }

  showFillFakePrelimModal(): void {
    this.fakePrelimError = '';
    this.incompletePrelimGamesForFake = this.prelimGames.filter(
      (g) => g.gameType === GameType.Preliminary && g.status !== GameStatus.Completed
    );
    this.fakePrelimModal.open();
  }

  confirmFillFakePrelimResults(): void {
    const games = this.incompletePrelimGamesForFake;
    if (games.length === 0) {
      return;
    }
    if (this.gameTeams.length < 2) {
      this.fakePrelimError =
        'Need at least two game teams (same list as Edit Game). Wait for teams to load or refresh the page.';
      return;
    }
    this.fakePrelimError = '';
    this.fakePrelimSaving = true;
    from(games)
      .pipe(concatMap((game) => this.resultService.updateResult(game.gameResultId, this.buildFakeSaveRequest(game))))
      .subscribe({
        complete: () => {
          this.fakePrelimSaving = false;
          this.fakePrelimModal.close();
          this.notificationLog.add({
            level: 'info',
            text: `Filled ${games.length} preliminary game(s) with fake results.`
          });
          this.loadGames();
          this.refreshTournamentAdminMenuFlags();
        },
        error: (err: unknown) => {
          this.fakePrelimSaving = false;
          this.fakePrelimError = getHttpErrorMessage(err, 'Could not save fake results.');
        }
      });
  }

  private randomInt(min: number, max: number): number {
    return Math.floor(Math.random() * (max - min + 1)) + min;
  }

  /** Scores in [0, 30], never equal (API rejects ties for completed games). */
  private randomDistinctScores(): [number, number] {
    let s1 = this.randomInt(0, 30);
    let s2 = this.randomInt(0, 30);
    if (s1 === s2) {
      s2 = s1 >= 30 ? s1 - 1 : s1 + 1;
    }
    return [s1, s2];
  }

  /**
   * Two random distinct teams from the global game-teams list (matches edit-game UI source).
   * Each call returns a new pair so every fake-filled game can differ.
   */
  private pickTwoRandomDistinctTeams(): [IGameTeam, IGameTeam] {
    const n = this.gameTeams.length;
    const i = this.randomInt(0, n - 1);
    let j = this.randomInt(0, n - 1);
    while (j === i) {
      j = this.randomInt(0, n - 1);
    }
    return [this.gameTeams[i], this.gameTeams[j]];
  }

  private buildFakeSaveRequest(game: IGameResult): ISaveGameResultRequest {
    const [s1, s2] = this.randomDistinctScores();
    const [team1, team2] = this.pickTwoRandomDistinctTeams();
    return {
      gameResultId: game.gameResultId,
      tournamentId: game.tournamentId,
      gameType: GameType.Preliminary,
      status: GameStatus.Completed,
      player1: {
        playerId: game.player1.playerId,
        playerName: game.player1.playerName,
        teamName: team1.teamName,
        gameTeamId: team1.gameTeamId,
        score: s1,
        passingYards: this.randomInt(1, 100),
        rushingYards: this.randomInt(1, 100)
      },
      player2: {
        playerId: game.player2.playerId,
        playerName: game.player2.playerName,
        teamName: team2.teamName,
        gameTeamId: team2.gameTeamId,
        score: s2,
        passingYards: this.randomInt(1, 100),
        rushingYards: this.randomInt(1, 100)
      }
    };
  }

  showReset(): void {
    this.resetError = '';
    this.resetModal.open();
  }

  showRestartPhaseModal(): void {
    this.restartPhaseError = '';
    this.restartPhaseModal.open();
  }

  confirmRestartTournamentPhase(): void {
    const tid = this.tournament!.tournamentId;
    const request: IResetTournamentRequest = { tournamentId: tid };
    this.restartPhaseError = '';
    this.restartingPhase = true;
    this.tournamentService.resetTournamentPhase(tid, request).subscribe({
      next: () => {
        this.restartingPhase = false;
        this.restartPhaseModal.close();
        this.notificationLog.add({
          level: 'info',
          text: 'Bracket phase cleared. Status set to preliminaries; preliminary games unchanged.'
        });
        this.tournamentService.getTournament(tid).subscribe((t) => {
          this.tournament = t;
          this.refreshTournamentAdminMenuFlags();
          this.tournamentCompleted = t.status === TournamentStatus.Completed;
          this.pendingTabSlug = 'preliminaries';
          this.loadGames({
            endPageLoad: true,
            afterLoaded: () => {
              this.selectedIndex = 0;
              void this.router.navigate(['/tournaments', tid, 'preliminaries']);
            }
          });
        });
      },
      error: () => {
        this.restartingPhase = false;
        this.restartPhaseError = 'Could not restart tournament phase.';
      }
    });
  }

  resetTournament(): void {
    const request: IResetTournamentRequest = {
      tournamentId: this.tournament!.tournamentId
    };

    this.resetError = '';
    this.resettingTournament = true;
    this.tournamentService.resetTournament(this.tournament!.tournamentId, request).subscribe({
      next: () => {
        this.resettingTournament = false;
        this.resetModal.close();
        this.lastLoadedTournamentId = null;
        const tid = +this.route.snapshot.paramMap.get('id')!;
        this.loadTournamentData(tid);
      },
      error: () => {
        this.resettingTournament = false;
        this.resetError = 'Reset failed.';
      }
    });
  }
}  
