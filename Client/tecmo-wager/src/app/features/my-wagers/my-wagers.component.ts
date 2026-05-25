import { Component, OnInit, inject, signal, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { interval } from 'rxjs';
import { StarFlankedTitleComponent } from '../../shared/components/star-flanked-title/star-flanked-title.component';
import { WagerHistoryCardComponent } from '../../shared/components/wager-history-card/wager-history-card.component';
import { WagerApiService } from '../../core/services/wager-api.service';
import { ActiveTournamentService } from '../../core/services/active-tournament.service';
import { MyWager } from '../../core/models/my-wager.model';
import { WagerAuditEntry } from '../../core/models/wager-audit-entry.model';
import {
  AuditGameInfo,
  buildViewFromMyWager,
  indexAuditByWagerId,
  indexGamesFromBoard,
  indexGamesFromWagers,
  indexWagersById,
  mergeGameInfoMaps,
  WagerHistoryCardView
} from '../../core/utils/wager-audit-display.util';

@Component({
  selector: 'app-my-wagers',
  standalone: true,
  imports: [StarFlankedTitleComponent, WagerHistoryCardComponent],
  templateUrl: './my-wagers.component.html',
  styleUrl: './my-wagers.component.less'
})
export class MyWagersComponent implements OnInit {
  private static readonly refreshMs = 30_000;

  private wagerApi = inject(WagerApiService);
  readonly activeTournament = inject(ActiveTournamentService);
  private destroyRef = inject(DestroyRef);

  loading = signal(true);
  error = signal('');
  wagers = signal<MyWager[]>([]);
  auditByWagerId = signal<Map<number, WagerAuditEntry[]>>(new Map());
  gamesById = signal<Map<number, AuditGameInfo>>(new Map());

  activeWagers = computed(() => this.wagers().filter((w) => w.status === 'Pending'));

  settledWagers = computed(() => this.wagers().filter((w) => w.status !== 'Pending'));

  showEmpty = computed(
    () => !this.loading() && !this.error() && this.wagers().length === 0
  );

  ngOnInit(): void {
    void this.load();
    interval(MyWagersComponent.refreshMs)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => void this.load({ silent: true }));
  }

  async load(options?: { silent?: boolean }): Promise<void> {
    const silent = options?.silent === true;
    if (!silent) {
      this.loading.set(true);
      this.error.set('');
    }
    try {
      await this.activeTournament.refresh();
      const tid = this.activeTournament.tournamentId();
      if (tid == null) {
        this.wagers.set([]);
        this.auditByWagerId.set(new Map());
        this.gamesById.set(new Map());
        if (!silent) {
          this.loading.set(false);
        }
        return;
      }
      const [list, auditRows, board] = await Promise.all([
        this.wagerApi.getMyWagers(tid),
        this.wagerApi.getMyAudit(tid),
        this.wagerApi.getGamesBoard().catch(() => null)
      ]);
      this.wagers.set(list);
      this.auditByWagerId.set(indexAuditByWagerId(auditRows));
      const gameMaps = [indexGamesFromWagers(list)];
      if (board) {
        gameMaps.push(indexGamesFromBoard(board));
      }
      this.gamesById.set(mergeGameInfoMaps(...gameMaps));
      if (silent) {
        this.error.set('');
      }
    } catch (e) {
      if (!silent) {
        this.error.set(e instanceof Error ? e.message : 'Failed to load wagers.');
      }
    } finally {
      if (!silent) {
        this.loading.set(false);
      }
    }
  }

  cardView(w: MyWager): WagerHistoryCardView {
    return buildViewFromMyWager(
      w,
      this.auditByWagerId(),
      indexWagersById(this.wagers()),
      this.gamesById(),
      'activity'
    );
  }
}
