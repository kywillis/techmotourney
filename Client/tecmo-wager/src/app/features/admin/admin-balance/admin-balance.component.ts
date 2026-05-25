import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { StarFlankedTitleComponent } from '../../../shared/components/star-flanked-title/star-flanked-title.component';
import {
  WagerAdminApiService,
  WagerBalanceAdminRequest
} from '../../../core/services/wager-admin-api.service';
import {
  AdminReturnNav,
  parseAdminReturnNav
} from '../../../core/utils/admin-return-nav.util';

@Component({
  selector: 'app-admin-balance',
  standalone: true,
  imports: [FormsModule, DecimalPipe, RouterLink, StarFlankedTitleComponent],
  templateUrl: './admin-balance.component.html',
  styleUrl: './admin-balance.component.less'
})
export class AdminBalanceComponent implements OnInit {
  private adminApi = inject(WagerAdminApiService);
  private route = inject(ActivatedRoute);

  players = signal<{ playerId: number; fullName: string; balance: number }[]>([]);
  listLoading = signal(true);
  listError = signal('');

  selectedPlayerId: number | null = null;
  action: 'Set' | 'Add' | 'SetToZero' = 'Add';
  amount: number | null = null;
  submitting = signal(false);
  message = signal('');
  error = signal('');
  returnNav = signal<AdminReturnNav | null>(null);

  private loadGeneration = 0;

  ngOnInit(): void {
    this.returnNav.set(parseAdminReturnNav(this.route.snapshot.queryParamMap));
    void this.loadPlayers();
  }

  get currentBalance(): number | null {
    const id = this.selectedPlayerId;
    if (id == null) return null;
    return this.players().find((p) => p.playerId === id)?.balance ?? null;
  }

  private async loadPlayers(): Promise<void> {
    const gen = ++this.loadGeneration;
    this.listLoading.set(true);
    this.listError.set('');
    try {
      const rows = await this.adminApi.getPlayersForBalanceAdmin();
      if (gen !== this.loadGeneration) return;
      this.players.set(rows);
      const qp = this.route.snapshot.queryParamMap.get('playerId');
      if (qp) {
        const fromQuery = parseInt(qp, 10);
        if (Number.isFinite(fromQuery) && rows.some((p) => p.playerId === fromQuery)) {
          this.selectedPlayerId = fromQuery;
        }
      }
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
      await this.loadPlayers();
    } catch (e) {
      this.error.set(e instanceof Error ? e.message : 'Update failed.');
    } finally {
      this.submitting.set(false);
    }
  }
}
