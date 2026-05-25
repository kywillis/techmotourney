import { Injectable, inject, signal } from '@angular/core';
import { WagerApiService } from './wager-api.service';

/** Shared active tournament label for header + consumers that call refresh(). */
@Injectable({ providedIn: 'root' })
export class ActiveTournamentService {
  private wagerApi = inject(WagerApiService);

  private readonly name = signal('');
  private readonly id = signal<number | null>(null);

  readonly tournamentName = this.name.asReadonly();
  readonly tournamentId = this.id.asReadonly();

  async refresh(): Promise<void> {
    try {
      const t = await this.wagerApi.getActiveTournament();
      if (!t?.tournamentId) {
        this.name.set('');
        this.id.set(null);
        return;
      }
      this.name.set(t.name ?? '');
      this.id.set(t.tournamentId);
    } catch {
      this.name.set('');
      this.id.set(null);
    }
  }
}
