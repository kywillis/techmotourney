import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { StarFlankedTitleComponent } from '../../../shared/components/star-flanked-title/star-flanked-title.component';
import {
  WagerAdminApiService,
  WagerTournamentSnapshot
} from '../../../core/services/wager-admin-api.service';
import { AdminTournamentContextService } from '../../../core/services/admin-tournament-context.service';
import { formatNetSummaryLine, formatBookUsd } from '../../../core/utils/book-money.util';

function isHouseNetZero(net: number): boolean {
  return net === 0 || Object.is(net, -0);
}

@Component({
  selector: 'app-admin-snapshot',
  standalone: true,
  imports: [RouterLink, StarFlankedTitleComponent],
  templateUrl: './admin-snapshot.component.html',
  styleUrl: './admin-snapshot.component.less'
})
export class AdminSnapshotComponent implements OnInit {
  private adminApi = inject(WagerAdminApiService);
  private adminTournament = inject(AdminTournamentContextService);

  bookUsd = formatBookUsd;
  netLine = formatNetSummaryLine;
  isHouseNetZero = isHouseNetZero;

  data = signal<WagerTournamentSnapshot | null>(null);
  loading = signal(true);
  error = signal('');

  currentTid = signal<number | null>(null);
  hasSelection = computed(() => this.currentTid() != null);

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set('');
    try {
      await this.adminTournament.ensureLoaded();
      const tid = this.adminTournament.tournamentId();
      this.currentTid.set(tid);
      if (tid == null) {
        this.data.set(null);
        this.error.set('No tournament selected.');
        return;
      }
      const snap = await this.adminApi.getWagerSnapshot(tid);
      this.data.set(snap);
    } catch (e) {
      this.data.set(null);
      this.error.set(e instanceof Error ? e.message : 'Failed to load snapshot.');
    } finally {
      this.loading.set(false);
    }
  }
}
