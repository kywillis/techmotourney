import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { WagerAdminApiService } from '../../../core/services/wager-admin-api.service';
import { AdminPlayerLinkListItem, PendingActivation } from '../../../core/models/pending-activation.model';

@Component({
  selector: 'app-admin-pending-players',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './admin-pending-players.component.html',
  styleUrl: './admin-pending-players.component.less'
})
export class AdminPendingPlayersComponent implements OnInit {
  private adminApi = inject(WagerAdminApiService);

  rows = signal<PendingActivation[]>([]);
  linkablePlayers = signal<AdminPlayerLinkListItem[]>([]);
  loading = signal(true);
  busyPendingId = signal<number | null>(null);
  error = signal('');
  resultMessage = signal('');
  /** pendingActivationId -> selected player id string (empty = none) */
  selectedPlayerIdByPending = signal<Record<number, string>>({});

  ngOnInit(): void {
    void this.load();
  }

  selectedPlayerId(pendingId: number): string {
    return this.selectedPlayerIdByPending()[pendingId] ?? '';
  }

  onPlayerSelectChange(pendingId: number, value: string): void {
    this.selectedPlayerIdByPending.update((m) => ({ ...m, [pendingId]: value }));
  }

  onPlayerSelectNativeChange(event: Event, pendingId: number): void {
    const el = event.target as HTMLSelectElement | null;
    this.onPlayerSelectChange(pendingId, el?.value ?? '');
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set('');
    try {
      const [list, linkable] = await Promise.all([
        this.adminApi.getPendingActivations(false),
        this.adminApi.getPlayersEligibleForGoogleLink()
      ]);
      this.rows.set(list);
      this.linkablePlayers.set(linkable);
    } catch (e) {
      this.error.set(this.httpErrorMessage(e, 'Failed to load pending signups.'));
    } finally {
      this.loading.set(false);
    }
  }

  async linkRow(r: PendingActivation): Promise<void> {
    const raw = this.selectedPlayerId(r.pendingActivationId);
    const playerId = Number(raw);
    if (!Number.isFinite(playerId) || playerId < 1) return;

    this.busyPendingId.set(r.pendingActivationId);
    this.resultMessage.set('');
    this.error.set('');
    try {
      await this.adminApi.linkPendingToPlayer(r.pendingActivationId, playerId);
      this.resultMessage.set(`Linked ${r.email || r.fullName || 'signup'} to player #${playerId}.`);
      await this.load();
      this.selectedPlayerIdByPending.update((m) => {
        const next = { ...m };
        delete next[r.pendingActivationId];
        return next;
      });
    } catch (e) {
      this.error.set(this.httpErrorMessage(e, 'Link failed.'));
    } finally {
      this.busyPendingId.set(null);
    }
  }

  async createNewRow(r: PendingActivation): Promise<void> {
    this.busyPendingId.set(r.pendingActivationId);
    this.resultMessage.set('');
    this.error.set('');
    try {
      await this.adminApi.activatePendingCreateNew(r.pendingActivationId, {
        fullName: r.fullName,
        emailAddress: r.email,
        profilePic: r.requestedProfilePic
      });
      this.resultMessage.set(`Created new player for ${r.email || r.fullName || 'signup'}.`);
      await this.load();
    } catch (e) {
      this.error.set(this.httpErrorMessage(e, 'Create new player failed.'));
    } finally {
      this.busyPendingId.set(null);
    }
  }

  rowBusy(pendingId: number): boolean {
    return this.busyPendingId() === pendingId;
  }

  private httpErrorMessage(e: unknown, fallback: string): string {
    if (e instanceof HttpErrorResponse) {
      const body = e.error;
      if (typeof body === 'string' && body.trim()) return body.trim();
      if (body && typeof body === 'object') {
        const o = body as Record<string, unknown>;
        const msg =
          o['errorMessage'] ?? o['ErrorMessage'] ?? o['message'] ?? o['Message'];
        if (typeof msg === 'string' && msg.trim()) return msg.trim();
      }
      return e.message || fallback;
    }
    return e instanceof Error ? e.message : fallback;
  }
}
