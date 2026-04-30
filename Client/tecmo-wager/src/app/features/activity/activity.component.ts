import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { StarFlankedTitleComponent } from '../../shared/components/star-flanked-title/star-flanked-title.component';
import { WagerApiService } from '../../core/services/wager-api.service';
import { WagerAuditEntry } from '../../core/models/wager-audit-entry.model';
import { TournamentSummary } from '../../core/models/tournament-summary.model';

@Component({
  selector: 'app-activity',
  standalone: true,
  imports: [DatePipe, RouterLink, StarFlankedTitleComponent],
  templateUrl: './activity.component.html',
  styleUrl: './activity.component.less'
})
export class ActivityComponent implements OnInit {
  private wagerApi = inject(WagerApiService);

  tournamentId = signal<number | null>(null);
  summary = signal<TournamentSummary | null>(null);
  entries = signal<WagerAuditEntry[]>([]);
  loading = signal(true);
  error = signal('');

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set('');
    try {
      const tournament = await this.wagerApi.getActiveTournament();
      if (!tournament?.tournamentId) {
        this.tournamentId.set(null);
        this.summary.set(null);
        this.entries.set([]);
        this.loading.set(false);
        return;
      }
      const tid = tournament.tournamentId;
      this.tournamentId.set(tid);

      const [auditRows, summ] = await Promise.all([
        this.wagerApi.getMyAudit(tid),
        this.wagerApi.getTournamentSummary(tid)
      ]);
      this.entries.set(auditRows);
      this.summary.set(summ);
    } catch (e) {
      this.error.set(e instanceof Error ? e.message : 'Failed to load activity.');
    } finally {
      this.loading.set(false);
    }
  }

  actionLabel(action: string): string {
    switch (action) {
      case 'PlaceWager':
        return 'Wager Placed';
      case 'CancelWager':
      case 'AdminCancelWager':
        return 'Wager Cancelled';
      case 'BalanceAdd':
        return 'Funds added';
      case 'BalanceSet':
        return 'Balance set';
      case 'BalanceSetToZero':
        return 'Balance cleared';
      default:
        return action.replace(/([A-Z])/g, ' $1').trim();
    }
  }

  amountDetail(e: WagerAuditEntry): string | null {
    switch (e.action) {
      case 'PlaceWager':
        if (e.amount == null || Number.isNaN(e.amount)) return null;
        return `Stake ${this.formatMoney(Math.abs(e.amount))}`;
      case 'CancelWager':
        if (e.amount == null || Number.isNaN(e.amount)) return null;
        return `Refund ${this.formatMoney(Math.abs(e.amount))}`;
      case 'BalanceAdd':
        if (e.amount == null || Number.isNaN(e.amount)) return null;
        return `Added ${this.formatMoney(Math.abs(e.amount))}`;
      case 'BalanceSet':
        if (e.amount == null || Number.isNaN(e.amount)) return null;
        return `Set to ${this.formatMoney(Math.abs(e.amount))}`;
      case 'BalanceSetToZero':
        return 'Set to $0';
      default:
        if (e.amount == null || Number.isNaN(e.amount)) return null;
        return this.formatMoney(Math.abs(e.amount));
    }
  }

  formatMoney(n: number): string {
    const r = Math.round(n * 100) / 100;
    return Number.isInteger(r) ? `$${r}` : `$${r.toFixed(2)}`;
  }

  showGameLink(e: WagerAuditEntry): boolean {
    return (
      e.gameResultId != null &&
      e.gameResultId > 0 &&
      (e.action === 'PlaceWager' || e.action === 'CancelWager' || e.action === 'AdminCancelWager')
    );
  }

  hasBalanceAfter(e: WagerAuditEntry): boolean {
    return e.balanceAfter != null && !Number.isNaN(e.balanceAfter);
  }

  formattedBalanceAfter(e: WagerAuditEntry): string {
    if (e.balanceAfter == null || Number.isNaN(e.balanceAfter)) return '';
    return this.formatMoney(e.balanceAfter);
  }
}
