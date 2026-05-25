import { Injectable, inject, signal } from '@angular/core';
import { WagerApiService } from './wager-api.service';
import { Tournament } from '../models/tournament.model';

const STORAGE_KEY = 'tecmo-wager.admin-tournament-id';

/** Admin-only: selected tournament for pending wagers, games, and odds (session-persisted). */
@Injectable({ providedIn: 'root' })
export class AdminTournamentContextService {
  private wagerApi = inject(WagerApiService);

  private initialized = false;
  private readonly all = signal<Tournament[]>([]);
  private readonly selectedId = signal<number | null>(null);
  private readonly selectedName = signal('');

  readonly tournaments = this.all.asReadonly();
  readonly tournamentId = this.selectedId.asReadonly();
  readonly tournamentName = this.selectedName.asReadonly();

  /** Idempotent: loads tournament list and resolves selection (stored → active → first). */
  async ensureLoaded(): Promise<void> {
    if (this.initialized) return;
    this.initialized = true;
    let list: Tournament[] = [];
    try {
      list = await this.wagerApi.getTournaments();
    } catch {
      list = [];
    }
    this.all.set(list);

    let active: Awaited<ReturnType<WagerApiService['getActiveTournament']>> = null;
    try {
      active = await this.wagerApi.getActiveTournament();
    } catch {
      active = null;
    }

    let stored: number | null = null;
    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      if (raw) {
        const n = parseInt(raw, 10);
        if (Number.isFinite(n) && n > 0) stored = n;
      }
    } catch {
      /* private mode */
    }

    let id =
      stored && list.some((t) => t.tournamentId === stored)
        ? stored
        : active?.tournamentId && list.some((t) => t.tournamentId === active.tournamentId)
          ? active.tournamentId
          : list[0]?.tournamentId ?? null;

    this.applySelection(id);
  }

  selectTournament(tournamentId: number): void {
    try {
      sessionStorage.setItem(STORAGE_KEY, String(tournamentId));
    } catch {
      /* ignore */
    }
    this.applySelection(tournamentId);
  }

  private applySelection(id: number | null): void {
    this.selectedId.set(id);
    const t = this.all().find((x) => x.tournamentId === id);
    this.selectedName.set(t?.name?.trim() ? t.name : id != null ? `Tournament ${id}` : '');
  }
}
