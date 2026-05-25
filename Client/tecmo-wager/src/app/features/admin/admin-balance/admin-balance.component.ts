import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { StarFlankedTitleComponent } from '../../../shared/components/star-flanked-title/star-flanked-title.component';
import {
  WagerAdminApiService,
  WagerBalanceAdminRequest
} from '../../../core/services/wager-admin-api.service';
import { AdminTournamentContextService } from '../../../core/services/admin-tournament-context.service';

@Component({
  selector: 'app-admin-balance',
  standalone: true,
  imports: [FormsModule, DecimalPipe, RouterLink, StarFlankedTitleComponent],
  templateUrl: './admin-balance.component.html',
  styleUrl: './admin-balance.component.less'
})
export class AdminBalanceComponent {
  private adminApi = inject(WagerAdminApiService);
  private adminTournament = inject(AdminTournamentContextService);

  players = signal<{ playerId: number; fullName: string; balance: number }[]>([]);
  listLoading = signal(true);
  listError = signal('');

  selectedPlayerId: number | null = null;
  action: 'Set' | 'Add' | 'SetToZero' = 'Add';
  amount: number | null = null;
  submitting = signal(false);
  message = signal('');
  error = signal('');

  private loadGeneration = 0;

  constructor() {
    effect(() => {
      const tid = this.adminTournament.tournamentId();
      void this.loadPlayersForTournament(tid);
    });
  }

  get currentBalance(): number | null {
    const id = this.selectedPlayerId;
    if (id == null) return null;
    return this.players().find((p) => p.playerId === id)?.balance ?? null;
  }

  private async loadPlayersForTournament(tournamentId: number | null): Promise<void> {
    const gen = ++this.loadGeneration;

    if (tournamentId == null) {
      this.players.set([]);
      this.selectedPlayerId = null;
      this.listLoading.set(false);
      this.listError.set('Choose a tournament from the menu (Change tournament).');
      return;
    }

    this.listLoading.set(true);
    this.listError.set('');
    try {
      const rows = await this.adminApi.getPlayersForBalanceAdmin(tournamentId);
      if (gen !== this.loadGeneration) return;
      this.players.set(rows);
      if (this.selectedPlayerId != null && !rows.some((p) => p.playerId === this.selectedPlayerId)) {
        this.selectedPlayerId = null;
      }
    } catch (e) {
      if (gen !== this.loadGeneration) return;
      this.players.set([]);
      this.selectedPlayerId = null;
      this.listError.set(e instanceof Error ? e.message : 'Could not load players.');
    } finally {
      if (gen === this.loadGeneration) {
        this.listLoading.set(false);
      }
    }
  }

  onPlayerChange(): void {
    this.message.set('');
    this.error.set('');
  }

  async submit(): Promise<void> {
    this.error.set('');
    this.message.set('');
    const pid = this.selectedPlayerId;
    if (pid == null || pid < 1) {
      this.error.set('Select a player.');
      return;
    }
    const act = this.action;
    if ((act === 'Set' || act === 'Add') && (this.amount == null || Number.isNaN(Number(this.amount)))) {
      this.error.set('Amount required for Set or Add.');
      return;
    }
    this.submitting.set(true);
    try {
      const body: WagerBalanceAdminRequest = {
        playerId: pid,
        action: act,
        amount: act === 'SetToZero' ? null : Number(this.amount)
      };
      await this.adminApi.updatePlayerBalance(body);
      this.message.set('Balance updated.');
      await this.loadPlayersForTournament(this.adminTournament.tournamentId());
    } catch (e) {
      this.error.set(e instanceof Error ? e.message : 'Update failed.');
    } finally {
      this.submitting.set(false);
    }
  }
}
