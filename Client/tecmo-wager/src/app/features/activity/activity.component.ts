import { Component, OnInit, inject, signal } from '@angular/core';
import { StarFlankedTitleComponent } from '../../shared/components/star-flanked-title/star-flanked-title.component';
import { WagerHistoryCardComponent } from '../../shared/components/wager-history-card/wager-history-card.component';
import { WagerApiService } from '../../core/services/wager-api.service';
import { WagerAuditEntry } from '../../core/models/wager-audit-entry.model';
import { TournamentSummary } from '../../core/models/tournament-summary.model';
import { MyWager } from '../../core/models/my-wager.model';
import {
  AuditGameInfo,
  buildViewFromAuditEntry,
  formatWagerAuditMoney,
  indexGamesFromBoard,
  indexGamesFromWagers,
  indexWagersById,
  mergeGameInfoMaps,
  WagerHistoryCardView
} from '../../core/utils/wager-audit-display.util';

@Component({
  selector: 'app-activity',
  standalone: true,
  imports: [StarFlankedTitleComponent, WagerHistoryCardComponent],
  templateUrl: './activity.component.html',
  styleUrl: './activity.component.less'
})
export class ActivityComponent implements OnInit {
  private wagerApi = inject(WagerApiService);

  tournamentId = signal<number | null>(null);
  summary = signal<TournamentSummary | null>(null);
  entries = signal<WagerAuditEntry[]>([]);
  wagersById = signal<Map<number, MyWager>>(new Map());
  gamesById = signal<Map<number, AuditGameInfo>>(new Map());
  loading = signal(true);
  error = signal('');

  formatMoney = formatWagerAuditMoney;

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set('');
    try {
      const tournament = await this.wagerApi.getActiveTournament();
      if (!tournament?.tournamentId) {
        this.tournamentId.set(null);
        this.summary.set(null);
        this.entries.set([]);
        this.wagersById.set(new Map());
        this.gamesById.set(new Map());
        this.loading.set(false);
        return;
      }
      const tid = tournament.tournamentId;
      this.tournamentId.set(tid);

      const [auditRows, summ, wagers, board] = await Promise.all([
        this.wagerApi.getMyAudit(tid),
        this.wagerApi.getTournamentSummary(tid),
        this.wagerApi.getMyWagers(tid),
        this.wagerApi.getGamesBoard().catch(() => null)
      ]);
      this.entries.set(auditRows);
      this.summary.set(summ);
      this.wagersById.set(indexWagersById(wagers));
      const gameMaps = [indexGamesFromWagers(wagers)];
      if (board) {
        gameMaps.push(indexGamesFromBoard(board));
      }
      this.gamesById.set(mergeGameInfoMaps(...gameMaps));
    } catch (e) {
      this.error.set(e instanceof Error ? e.message : 'Failed to load activity.');
    } finally {
      this.loading.set(false);
    }
  }

  cardView(e: WagerAuditEntry): WagerHistoryCardView {
    return buildViewFromAuditEntry(e, this.wagersById(), this.gamesById(), 'activity');
  }
}
