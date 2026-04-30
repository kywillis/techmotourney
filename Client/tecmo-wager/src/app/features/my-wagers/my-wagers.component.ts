import { Component, OnInit, inject, signal, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { interval } from 'rxjs';
import { StarFlankedTitleComponent } from '../../shared/components/star-flanked-title/star-flanked-title.component';
import { WagerApiService } from '../../core/services/wager-api.service';
import { ActiveTournamentService } from '../../core/services/active-tournament.service';
import { MyWager } from '../../core/models/my-wager.model';
import { formatWagerPick, formatWagerStatus } from '../../core/utils/wager-display.util';
import { formatBookUsd } from '../../core/utils/book-money.util';

@Component({
  selector: 'app-my-wagers',
  standalone: true,
  imports: [RouterLink, StarFlankedTitleComponent],
  templateUrl: './my-wagers.component.html',
  styleUrl: './my-wagers.component.less'
})
export class MyWagersComponent implements OnInit {
  private static readonly refreshMs = 30_000;

  private wagerApi = inject(WagerApiService);
  readonly activeTournament = inject(ActiveTournamentService);
  private destroyRef = inject(DestroyRef);

  loading = signal(true);
  error = signal('');
  wagers = signal<MyWager[]>([]);

  activeWagers = computed(() =>
    this.wagers().filter(w => w.status === 'Pending')
  );

  settledWagers = computed(() =>
    this.wagers().filter(w => w.status !== 'Pending')
  );

  showEmpty = computed(
    () => !this.loading() && !this.error() && this.wagers().length === 0
  );

  ngOnInit(): void {
    void this.load();
    interval(MyWagersComponent.refreshMs)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => void this.load({ silent: true }));
  }

  async load(options?: { silent?: boolean }): Promise<void> {
    const silent = options?.silent === true;
    if (!silent) {
      this.loading.set(true);
      this.error.set('');
    }
    try {
      await this.activeTournament.refresh();
      const tid = this.activeTournament.tournamentId();
      if (tid == null) {
        this.wagers.set([]);
        if (!silent) {
          this.loading.set(false);
        }
        return;
      }
      const list = await this.wagerApi.getMyWagers(tid);
      this.wagers.set(list);
      if (silent) {
        this.error.set('');
      }
    } catch (e) {
      if (!silent) {
        this.error.set(e instanceof Error ? e.message : 'Failed to load wagers.');
      }
    } finally {
      if (!silent) {
        this.loading.set(false);
      }
    }
  }

  matchup(w: MyWager): string {
    const a = (w.player1Name || '').trim() || '—';
    const b = (w.player2Name || '').trim() || '—';
    return `${a} vs ${b}`;
  }

  pickLine(w: MyWager): string {
    const d = (w.pickDescription || '').trim();
    if (d) return d;
    return formatWagerPick(w.marketType, w.side, w.player1Name, w.player2Name);
  }

  stakeAndPayout(w: MyWager): string {
    const stakeTxt = formatBookUsd(w.stakeAmount);
    const payout = w.potentialPayout;
    if (payout == null || Number.isNaN(payout) || payout <= 0) {
      return `Stake ${stakeTxt}`;
    }
    return `Stake ${stakeTxt}, Payout ${formatBookUsd(payout)}`;
  }

  statusLabel(status: string): string {
    return formatWagerStatus(status);
  }

  canViewGame(w: MyWager): boolean {
    return w.status === 'Pending';
  }
}
