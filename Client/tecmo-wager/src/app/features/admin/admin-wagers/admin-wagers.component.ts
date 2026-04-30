import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { StarFlankedTitleComponent } from '../../../shared/components/star-flanked-title/star-flanked-title.component';
import { WagerAdminApiService } from '../../../core/services/wager-admin-api.service';
import { AdminTournamentContextService } from '../../../core/services/admin-tournament-context.service';
import { MyWager } from '../../../core/models/my-wager.model';
import { formatBookUsd } from '../../../core/utils/book-money.util';

@Component({
  selector: 'app-admin-wagers',
  standalone: true,
  imports: [RouterLink, StarFlankedTitleComponent],
  templateUrl: './admin-wagers.component.html',
  styleUrl: './admin-wagers.component.less'
})
export class AdminWagersComponent implements OnInit {
  private adminApi = inject(WagerAdminApiService);
  private adminTournament = inject(AdminTournamentContextService);

  bookUsd = formatBookUsd;

  wagers = signal<MyWager[]>([]);
  loading = signal(true);
  error = signal('');
  busyId = signal<number | null>(null);

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set('');
    try {
      await this.adminTournament.ensureLoaded();
      const tid = this.adminTournament.tournamentId();
      if (tid == null) {
        this.wagers.set([]);
        this.error.set('No tournament selected.');
        return;
      }
      const rows = await this.adminApi.getPendingWagers(tid);
      this.wagers.set(rows);
    } catch (e) {
      this.error.set(e instanceof Error ? e.message : 'Failed to load wagers.');
    } finally {
      this.loading.set(false);
    }
  }

  matchup(w: MyWager): string {
    const a = w.player1Name?.trim() || '—';
    const b = w.player2Name?.trim() || '—';
    return `${a} vs ${b}`;
  }

  async cancel(wagerId: number): Promise<void> {
    if (!confirm('Refund stake and cancel this wager for the player?')) return;
    this.busyId.set(wagerId);
    try {
      await this.adminApi.adminCancelWager(wagerId);
      await this.load();
    } catch (e) {
      this.error.set(e instanceof Error ? e.message : 'Cancel failed.');
    } finally {
      this.busyId.set(null);
    }
  }
}
