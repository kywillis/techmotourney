import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { StarFlankedTitleComponent } from '../../shared/components/star-flanked-title/star-flanked-title.component';
import { WagerApiService } from '../../core/services/wager-api.service';
import { ActiveTournamentService } from '../../core/services/active-tournament.service';
import { MyWager } from '../../core/models/my-wager.model';
import { formatWagerPick, formatWagerStatus } from '../../core/utils/wager-display.util';

@Component({
  selector: 'app-my-wagers',
  standalone: true,
  imports: [RouterLink, StarFlankedTitleComponent],
  templateUrl: './my-wagers.component.html',
  styleUrl: './my-wagers.component.less'
})
export class MyWagersComponent implements OnInit {
  private wagerApi = inject(WagerApiService);
  readonly activeTournament = inject(ActiveTournamentService);

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
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set('');
    try {
      await this.activeTournament.refresh();
      const tid = this.activeTournament.tournamentId();
      if (tid == null) {
        this.wagers.set([]);
        this.loading.set(false);
        return;
      }
      const list = await this.wagerApi.getMyWagers(tid);
      this.wagers.set(list);
    } catch (e) {
      this.error.set(e instanceof Error ? e.message : 'Failed to load wagers.');
    } finally {
      this.loading.set(false);
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
    const stake = w.stakeAmount;
    const stakeTxt = `$${Math.round(stake)}`;
    const payout = w.potentialPayout;
    if (payout == null || Number.isNaN(payout) || payout <= 0) {
      return `Stake ${stakeTxt}`;
    }
    const rounded = Math.round(payout * 100) / 100;
    const payoutTxt = Number.isInteger(rounded)
      ? `$${rounded}`
      : `$${rounded.toFixed(2)}`;
    return `Stake ${stakeTxt}, Payout ${payoutTxt}`;
  }

  statusLabel(status: string): string {
    return formatWagerStatus(status);
  }

  canViewGame(w: MyWager): boolean {
    return w.status === 'Pending';
  }
}
