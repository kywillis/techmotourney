import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { StarFlankedTitleComponent } from '../../../shared/components/star-flanked-title/star-flanked-title.component';
import { WagerAdminApiService } from '../../../core/services/wager-admin-api.service';

const SESSION_KEY_PLAYERS_SEARCH = 'tecmo-wager.admin-players-search';

@Component({
  selector: 'app-admin-players-list',
  standalone: true,
  imports: [DecimalPipe, FormsModule, RouterLink, StarFlankedTitleComponent],
  templateUrl: './admin-players-list.component.html',
  styleUrl: './admin-players-list.component.less'
})
export class AdminPlayersListComponent implements OnInit {
  private adminApi = inject(WagerAdminApiService);

  players = signal<{ playerId: number; fullName: string; balance: number }[]>([]);
  searchText = signal('');
  loading = signal(true);
  error = signal('');

  /** Client-side filter: full name must contain search text (case-insensitive). */
  filteredPlayers = computed(() => {
    const all = this.players();
    const q = this.searchText();
    if (!q) {
      return all;
    }
    const needle = q.toLowerCase();
    return all.filter((p) => p.fullName.toLowerCase().includes(needle));
  });

  ngOnInit(): void {
    this.restoreSearchFromSession();
    void this.load();
  }

  onSearchChange(value: string): void {
    this.searchText.set(value);
    this.persistSearchToSession(value);
  }

  private restoreSearchFromSession(): void {
    if (typeof sessionStorage === 'undefined') {
      return;
    }
    try {
      const stored = sessionStorage.getItem(SESSION_KEY_PLAYERS_SEARCH);
      if (stored != null) {
        this.searchText.set(stored);
      }
    } catch {
      /* private mode / quota */
    }
  }

  private persistSearchToSession(value: string): void {
    if (typeof sessionStorage === 'undefined') {
      return;
    }
    try {
      if (!value) {
        sessionStorage.removeItem(SESSION_KEY_PLAYERS_SEARCH);
      } else {
        sessionStorage.setItem(SESSION_KEY_PLAYERS_SEARCH, value);
      }
    } catch {
      /* ignore */
    }
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set('');
    try {
      this.players.set(await this.adminApi.getPlayersForBalanceAdmin());
    } catch (e) {
      this.players.set([]);
      this.error.set(e instanceof Error ? e.message : 'Could not load players.');
    } finally {
      this.loading.set(false);
    }
  }
}
