import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  signal,
  computed,
  effect,
  untracked
} from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { StarFlankedTitleComponent } from '../../../shared/components/star-flanked-title/star-flanked-title.component';
import { WagerApiService } from '../../../core/services/wager-api.service';
import { WagerAuthService } from '../../../core/services/wager-auth.service';
import { BettableGame } from '../../../core/models/bettable-game.model';
import { PlaceWagerRequest, WagerMarketType, WagerSide } from '../../../core/models/place-wager-request.model';
import {
  profitFromAmericanOdds,
  maxStakeForAmericanOddsWinCap
} from '../../../core/utils/american-odds.util';
import {
  bookStakeUpperBound,
  maxStakeForImbalance,
  wouldViolateMarketImbalance
} from '../../../core/utils/market-imbalance.util';

/** Max dollars at risk on spread / O/U. */
const HOUSE_MAX_RISK_SPREAD_OU = 40;
/** Max profit (to win) on money line; max risk is derived from the line. */
const HOUSE_MAX_WIN_MONEYLINE = 40;
const MIN_STAKE = 1;
const POLL_MS = 10_000;

@Component({
  selector: 'app-game-detail',
  standalone: true,
  imports: [DecimalPipe, StarFlankedTitleComponent],
  templateUrl: './game-detail.component.html',
  styleUrl: './game-detail.component.less'
})
export class GameDetailComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private wagerApi = inject(WagerApiService);
  private auth = inject(WagerAuthService);

  private gameResultId = 0;
  private pollTimer: ReturnType<typeof setInterval> | null = null;
  private onVisibility = (): void => {
    if (document.visibilityState === 'visible' && this.gameResultId > 0) {
      void this.refreshGameSilently();
    }
  };

  game = signal<BettableGame | null>(null);
  loading = signal(true);
  error = signal('');
  placeError = signal('');
  placing = signal(false);
  selectedMarket = signal<WagerMarketType>('Spread');
  selectedSide = signal<WagerSide>('Player1Spread');
  /** 0 = no amount entered yet (empty field + placeholder). */
  stakeAmount = signal<number>(0);
  /** User typed more than allowed max; show stakeLimitWhy() near the field. */
  stakeOverMaxHintShown = signal(false);

  balance = this.auth.balance;

  /** Logged-in user is one of the two competitors. */
  isParticipantInGame = computed(() => {
    const g = this.game();
    const pid = this.auth.currentAuth()?.playerId;
    if (!g || pid == null) return false;
    return pid === g.player1Id || pid === g.player2Id;
  });

  /** Strict: only true when API explicitly allows betting (missing flag = closed for safety). */
  isOpenForBettingNow = computed(() => this.game()?.isOpenForBetting === true);

  pageTitle = computed(() => (this.isOpenForBettingNow() ? 'Place wager' : 'Game lines'));

  gameStatusDebugLine = computed(() => {
    const g = this.game();
    if (!g) return '';
    const st = g.gameStatus?.trim() || '—';
    return `Status: ${st}`;
  });

  finalScoreSummary = computed(() => {
    const g = this.game();
    if (!g || g.player1Score == null || g.player2Score == null) return null;
    return `Final: ${g.player1Name} ${g.player1Score} – ${g.player2Score} ${g.player2Name}`;
  });

  spreadDisplay = computed(() => {
    const g = this.game();
    if (!g) return { p1: 0, p2: 0 };
    const o = g.odds;
    const mag = Math.abs(o.spread ?? 0);
    const favored = o.favoredPlayerId;
    return {
      p1: favored === g.player1Id ? -mag : mag,
      p2: favored === g.player2Id ? -mag : mag
    };
  });

  floorBalance = computed(() => {
    const bal = this.balance();
    if (!Number.isFinite(bal) || bal <= 0) return 0;
    return Math.floor(bal);
  });

  /** House cap on risk for this market/side (spread/O/U = $40; ML = stake that keeps win ≤ $40). */
  houseStakeCap = computed(() => {
    const g = this.game();
    const m = this.selectedMarket();
    if (!g || m === 'Spread' || m === 'OverUnder') {
      return HOUSE_MAX_RISK_SPREAD_OU;
    }
    const side = this.selectedSide();
    const line =
      side === 'Player1ML' ? g.odds.moneyLinePlayer1 : g.odds.moneyLinePlayer2;
    if (line == null) return HOUSE_MAX_RISK_SPREAD_OU;
    return maxStakeForAmericanOddsWinCap(HOUSE_MAX_WIN_MONEYLINE, line);
  });

  /** Also limited by book imbalance (same rule as API). */
  effectiveMaxStake = computed(() =>
    maxStakeForImbalance(
      this.game()?.marketDepth,
      this.selectedMarket(),
      this.selectedSide(),
      this.houseStakeCap(),
      this.floorBalance()
    )
  );

  marketBook = computed(() => {
    const g = this.game();
    const d = g?.marketDepth;
    if (!g || !d) return null;
    const m = this.selectedMarket();
    if (m === 'Spread') {
      return {
        labelA: g.player1Name,
        labelB: g.player2Name,
        amountA: d.spreadPlayer1,
        amountB: d.spreadPlayer2,
        maxImbalance: d.maxMarketImbalance
      };
    }
    if (m === 'OverUnder') {
      return {
        labelA: 'Over',
        labelB: 'Under',
        amountA: d.over,
        amountB: d.under,
        maxImbalance: d.maxMarketImbalance
      };
    }
    return {
      labelA: g.player1Name,
      labelB: g.player2Name,
      amountA: d.moneyLinePlayer1,
      amountB: d.moneyLinePlayer2,
      maxImbalance: d.maxMarketImbalance
    };
  });

  imbalanceBlocksWager = computed(() =>
    wouldViolateMarketImbalance(
      this.game()?.marketDepth,
      this.selectedMarket(),
      this.selectedSide(),
      this.stakeAmount()
    )
  );

  /**
   * Skew = |A−B| / total (0–100). Bar from center toward heavier side; color by skew bands.
   */
  marketBookDisplay = computed(() => {
    const b = this.marketBook();
    if (!b) return null;
    const total = b.amountA + b.amountB;
    const skewPercent =
      total > 0 ? (Math.abs(b.amountA - b.amountB) / total) * 100 : 0;
    /** Width as % of full track (max half-width when skew 100%). */
    const barWidthPct = (skewPercent / 100) * 50;
    const barDirection =
      b.amountA > b.amountB ? 'left' : b.amountB > b.amountA ? 'right' : 'tie';
    const barColorTier =
      skewPercent <= 50 ? 'green' : skewPercent <= 80 ? 'yellow' : 'red';
    return {
      ...b,
      total,
      skewPercent,
      barWidthPct,
      barDirection,
      barColorTier,
      /** Colored fill only when there is money and a lean; track always shown in template. */
      showBarFill: total > 0 && skewPercent > 0
    };
  });

  /**
   * The opposite side we need action on (spread / ML with line, or Over/Under with total).
   * Used in friendly "paused until…" copy.
   */
  counterPickLabel = computed((): string => {
    const g = this.game();
    if (!g) return 'the other pick';
    const side = this.selectedSide();
    const sp = this.spreadDisplay();
    const fmtSpread = (n: number) => (n > 0 ? `+${n}` : `${n}`);
    const fmtMl = (n: number | null | undefined) => {
      if (n == null || Number.isNaN(n)) return '';
      return n > 0 ? `+${n}` : `${n}`;
    };
    const ou = g.odds.overUnder;
    const ouSuffix = ou != null && !Number.isNaN(ou) ? ` ${ou}` : '';

    switch (side) {
      case 'Player1Spread':
        return `${g.player2Name} ${fmtSpread(sp.p2)}`.trim();
      case 'Player2Spread':
        return `${g.player1Name} ${fmtSpread(sp.p1)}`.trim();
      case 'Player1ML':
        return `${g.player2Name} ${fmtMl(g.odds.moneyLinePlayer2)}`.trim();
      case 'Player2ML':
        return `${g.player1Name} ${fmtMl(g.odds.moneyLinePlayer1)}`.trim();
      case 'Over':
        return `Under${ouSuffix}`.trim();
      case 'Under':
        return `Over${ouSuffix}`.trim();
      default:
        return 'the other pick';
    }
  });

  pausedUntilMessage = computed(
    () =>
      `This pick is paused until someone bets on ${this.counterPickLabel()} to keep it even-ish.`
  );

  bookOnlyStakeCeiling = computed(() =>
    bookStakeUpperBound(
      this.game()?.marketDepth,
      this.selectedMarket(),
      this.selectedSide()
    )
  );

  /** Shown after user tries to enter more than effectiveMaxStake. */
  stakeLimitWhy = computed((): string => {
    const eff = this.effectiveMaxStake();
    const h = this.houseStakeCap();
    const b = this.floorBalance();
    const bk = this.bookOnlyStakeCeiling();
    const huge = Number.MAX_SAFE_INTEGER / 4;

    if (eff < MIN_STAKE) {
      return this.pausedUntilMessage();
    }

    const bookApplies = bk < huge;
    if (bookApplies && eff === bk && bk <= b && bk <= h) {
      return `The book only allows up to $${eff} on this side for now — more needs to land on ${this.counterPickLabel()} before this side can take extra action.`;
    }
    if (eff === h && h <= b && (!bookApplies || h <= bk)) {
      if (this.selectedMarket() === 'MoneyLine') {
        return `Money line cap: max stake is set so profit if you win stays at or under $${HOUSE_MAX_WIN_MONEYLINE} at this price — that's $${eff} here.`;
      }
      return `House cap on this market is $${eff} per bet.`;
    }
    if (eff === b && b <= h && (!bookApplies || b <= bk)) {
      return `Your balance is $${b}, so that's the most you can put down.`;
    }
    return `The most you can put on this pick right now is $${eff}.`;
  });

  /** Profit if the bet wins (stake returned separately). */
  toWinAmount = computed(() => {
    const stake = this.stakeAmount();
    const g = this.game();
    const market = this.selectedMarket();
    const side = this.selectedSide();
    if (!g || stake <= 0) return 0;
    if (market === 'Spread' || market === 'OverUnder') {
      return stake;
    }
    const line =
      side === 'Player1ML' ? g.odds.moneyLinePlayer1 : g.odds.moneyLinePlayer2;
    if (line == null) return stake;
    return profitFromAmericanOdds(stake, line);
  });

  totalReturnIfWin = computed(() => this.stakeAmount() + this.toWinAmount());

  payoutCaption = computed(() => {
    const market = this.selectedMarket();
    if (market === 'Spread' || market === 'OverUnder') {
      return 'even money';
    }
    const g = this.game();
    const side = this.selectedSide();
    if (!g) return '';
    const line =
      side === 'Player1ML' ? g.odds.moneyLinePlayer1 : g.odds.moneyLinePlayer2;
    return line != null ? `${line > 0 ? '+' : ''}${line}` : '';
  });

  canPlaceWager = computed(() => {
    if (!this.isOpenForBettingNow()) return false;
    if (this.isParticipantInGame()) return false;
    const stake = this.stakeAmount();
    const max = this.effectiveMaxStake();
    return (
      max >= MIN_STAKE &&
      stake >= MIN_STAKE &&
      stake <= max + 1e-9 &&
      !this.imbalanceBlocksWager()
    );
  });

  constructor() {
    effect(() => {
      const max = this.effectiveMaxStake();
      const stake = this.stakeAmount();
      if (max < MIN_STAKE) {
        untracked(() => this.stakeAmount.set(0));
        return;
      }
      if (stake > max) {
        untracked(() => this.stakeAmount.set(max));
      }
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('gameResultId');
    const gid = id ? parseInt(id, 10) : 0;
    if (!gid) {
      this.error.set('Invalid game');
      this.loading.set(false);
      return;
    }
    this.gameResultId = gid;
    void this.loadGame(gid, { initial: true });
    this.pollTimer = setInterval(() => {
      if (document.visibilityState !== 'visible') return;
      void this.refreshGameSilently();
    }, POLL_MS);
    document.addEventListener('visibilitychange', this.onVisibility);
  }

  ngOnDestroy(): void {
    if (this.pollTimer != null) {
      clearInterval(this.pollTimer);
      this.pollTimer = null;
    }
    document.removeEventListener('visibilitychange', this.onVisibility);
  }

  async loadGame(
    gameResultId: number,
    opts?: { initial?: boolean }
  ): Promise<void> {
    if (opts?.initial) {
      this.loading.set(true);
      this.error.set('');
    }
    try {
      const g = await this.wagerApi.getGameDetail(gameResultId);
      this.game.set(g);
      if (opts?.initial) {
        this.selectedMarket.set('Spread');
        this.selectedSide.set('Player1Spread');
        this.clampStakeToLimits();
      }
    } catch (e) {
      if (opts?.initial) {
        this.error.set(e instanceof Error ? e.message : 'Failed to load game.');
      }
    } finally {
      if (opts?.initial) {
        this.loading.set(false);
      }
    }
  }

  private async refreshGameSilently(): Promise<void> {
    if (this.gameResultId <= 0 || this.loading()) return;
    try {
      const g = await this.wagerApi.getGameDetail(this.gameResultId);
      this.game.set(g);
    } catch {
      /* keep last good snapshot */
    }
  }

  faceClass(profilePic: number): string {
    const n = profilePic && profilePic > 0 ? profilePic : 1;
    return `player-face player-face-${n}`;
  }

  /** American odds for display (+ on positives). */
  formatAmericanOdds(line: number | null | undefined): string {
    if (line == null || Number.isNaN(line)) return '';
    if (line > 0) return `+${line}`;
    return `${line}`;
  }

  /** Shown as placeholder in the risk field (label removed). */
  riskPlaceholder(): string {
    const max = this.effectiveMaxStake();
    if (!Number.isFinite(max)) {
      return 'Risk (min $1)';
    }
    return `Risk (min $1, max $${Math.round(max)})`;
  }

  setMarket(market: WagerMarketType): void {
    this.selectedMarket.set(market);
    if (market === 'Spread') this.selectedSide.set('Player1Spread');
    else if (market === 'OverUnder') this.selectedSide.set('Over');
    else this.selectedSide.set('Player1ML');
    this.placeError.set('');
    this.stakeOverMaxHintShown.set(false);
    this.stakeAmount.set(0);
  }

  setSide(side: WagerSide): void {
    this.selectedSide.set(side);
    this.placeError.set('');
    this.stakeOverMaxHintShown.set(false);
    this.stakeAmount.set(0);
  }

  /** Empty field when no stake entered; otherwise whole dollars. */
  stakeInputDisplay(): string | number {
    return this.stakeAmount() === 0 ? '' : this.stakeAmount();
  }

  setStakeFromInput(raw: string | number): void {
    const max = this.effectiveMaxStake();
    if (max < MIN_STAKE) {
      this.stakeAmount.set(0);
      this.stakeOverMaxHintShown.set(false);
      return;
    }

    const str = typeof raw === 'string' ? raw.trim() : String(raw);
    if (str === '' || str === '-') {
      this.stakeAmount.set(0);
      this.stakeOverMaxHintShown.set(false);
      return;
    }

    const num = typeof raw === 'number' ? raw : Number(str);
    if (!Number.isFinite(num)) {
      this.stakeAmount.set(0);
      this.stakeOverMaxHintShown.set(false);
      return;
    }

    const whole = Math.round(num);
    if (whole < MIN_STAKE) {
      this.stakeAmount.set(0);
      this.stakeOverMaxHintShown.set(false);
      return;
    }

    this.stakeOverMaxHintShown.set(whole > max + 1e-9);

    const clamped = Math.max(MIN_STAKE, Math.min(max, whole));
    this.stakeAmount.set(clamped);
  }

  private clampStakeToLimits(): void {
    const max = this.effectiveMaxStake();
    if (max < MIN_STAKE) {
      this.stakeAmount.set(0);
      return;
    }
    const s = this.stakeAmount();
    if (s === 0) {
      return;
    }
    if (s < MIN_STAKE || s > max) {
      const target = Math.round(s);
      this.stakeAmount.set(Math.max(MIN_STAKE, Math.min(max, target)));
    }
  }

  async placeWager(): Promise<void> {
    const g = this.game();
    if (!g || !this.canPlaceWager()) return;
    this.placeError.set('');
    this.placing.set(true);
    try {
      const request: PlaceWagerRequest = {
        gameResultId: g.gameResultId,
        marketType: this.selectedMarket(),
        side: this.selectedSide(),
        stakeAmount: this.stakeAmount()
      };
      await this.wagerApi.placeWager(request);
      await this.auth.authenticateWithGoogle(this.auth.getToken()!);
      this.router.navigate(['/wagers']);
    } catch (e: unknown) {
      const err = e as { error?: { errorMessage?: string; message?: string }; message?: string };
      const msg = err?.error?.errorMessage ?? err?.error?.message ?? err?.message ?? 'Failed to place wager.';
      this.placeError.set(msg);
    } finally {
      this.placing.set(false);
    }
  }

  /** Human-readable pick for action table (matches market button labels). */
  formatActionPick(side: string): string {
    const g = this.game();
    if (!g) return side;
    const sp = this.spreadDisplay();
    const fmtSp = (n: number) => (n > 0 ? `+${n}` : `${n}`);
    const fmtMl = (n: number | null | undefined) => {
      if (n == null || Number.isNaN(n)) return '';
      return n > 0 ? `+${n}` : `${n}`;
    };
    const ou = g.odds.overUnder;
    const ouSuffix = ou != null && !Number.isNaN(ou) ? ` ${ou}` : '';

    switch (side) {
      case 'Player1Spread':
        return `${g.player1Name} ${fmtSp(sp.p1)}`.trim();
      case 'Player2Spread':
        return `${g.player2Name} ${fmtSp(sp.p2)}`.trim();
      case 'Over':
        return `Over${ouSuffix}`.trim();
      case 'Under':
        return `Under${ouSuffix}`.trim();
      case 'Player1ML':
        return `${g.player1Name} ${fmtMl(g.odds.moneyLinePlayer1)}`.trim();
      case 'Player2ML':
        return `${g.player2Name} ${fmtMl(g.odds.moneyLinePlayer2)}`.trim();
      default:
        return side;
    }
  }
}
