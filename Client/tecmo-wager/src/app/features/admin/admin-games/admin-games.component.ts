import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { StarFlankedTitleComponent } from '../../../shared/components/star-flanked-title/star-flanked-title.component';
import {
  AdminGameResultRow,
  WagerAdminApiService
} from '../../../core/services/wager-admin-api.service';
import { AdminTournamentContextService } from '../../../core/services/admin-tournament-context.service';

@Component({
  selector: 'app-admin-games',
  standalone: true,
  imports: [RouterLink, StarFlankedTitleComponent],
  templateUrl: './admin-games.component.html',
  styleUrl: './admin-games.component.less'
})
export class AdminGamesComponent implements OnInit {
  private adminApi = inject(WagerAdminApiService);
  private adminTournament = inject(AdminTournamentContextService);

  games = signal<AdminGameResultRow[]>([]);
  loading = signal(true);
  error = signal('');

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
        this.games.set([]);
        this.error.set('No tournament selected.');
        return;
      }
      const rows = await this.adminApi.getTournamentResults(tid);
      this.games.set(rows);
    } catch (e) {
      this.error.set(e instanceof Error ? e.message : 'Failed to load games.');
    } finally {
      this.loading.set(false);
    }
  }

  label(g: AdminGameResultRow): string {
    const a = g.player1?.playerName?.trim() || `P${g.player1?.playerId ?? '?'}`;
    const b = g.player2?.playerName?.trim() || `P${g.player2?.playerId ?? '?'}`;
    return `${a} vs ${b} · ${g.status}`;
  }
}
