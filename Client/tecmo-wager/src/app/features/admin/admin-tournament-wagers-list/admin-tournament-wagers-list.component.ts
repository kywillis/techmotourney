import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { StarFlankedTitleComponent } from '../../../shared/components/star-flanked-title/star-flanked-title.component';
import { WagerAdminApiService } from '../../../core/services/wager-admin-api.service';
import { AdminTournamentContextService } from '../../../core/services/admin-tournament-context.service';
import { MyWager } from '../../../core/models/my-wager.model';
import { formatWagerPick, formatWagerStatus } from '../../../core/utils/wager-display.util';
import { formatBookUsd } from '../../../core/utils/book-money.util';
import {
  AdminReturnNav,
  parseAdminReturnNav
} from '../../../core/utils/admin-return-nav.util';

type Kind = 'player' | 'game';

@Component({
  selector: 'app-admin-tournament-wagers-list',
  standalone: true,
  imports: [RouterLink, StarFlankedTitleComponent],
  templateUrl: './admin-tournament-wagers-list.component.html',
  styleUrl: './admin-tournament-wagers-list.component.less'
})
export class AdminTournamentWagersListComponent implements OnInit {
  private adminApi = inject(WagerAdminApiService);
  private adminTournament = inject(AdminTournamentContextService);
  private route = inject(ActivatedRoute);

  bookUsd = formatBookUsd;
  statusText = formatWagerStatus;

  wagers = signal<MyWager[]>([]);

  /** Excludes cancelled everywhere (per product rules). */
  nonCancelledWagers = computed(() => this.wagers().filter((w) => w.status !== 'Cancelled'));

  wagersListOpen = computed(() => this.nonCancelledWagers().filter((w) => w.status === 'Pending'));

  wagersListSettled = computed(() => this.nonCancelledWagers().filter((w) => w.status !== 'Pending'));

  /** Wagers made, won/lost counts, total staked, player net (won/lost) from non‑cancelled rows only. */
  summary = computed(() => {
    const rows = this.nonCancelledWagers();
    let count = 0;
    let won = 0;
    let lost = 0;
    let totalWagered = 0;
    let netPnl = 0;
    for (const w of rows) {
      count += 1;
      totalWagered += w.stakeAmount;
      if (w.status === 'Won') {
        won += 1;
        netPnl += w.potentialPayout - w.stakeAmount;
      } else if (w.status === 'Lost') {
        lost += 1;
        netPnl -= w.stakeAmount;
      }
    }
    return { count, won, lost, totalWagered, netPnl };
  });

  headTitle = signal('Wager list');
  detailLine = signal('');
  loading = signal(true);
  error = signal('');
  returnNav = signal<AdminReturnNav | null>(null);

  ngOnInit(): void {
    this.returnNav.set(parseAdminReturnNav(this.route.snapshot.queryParamMap));
    void this.load();
  }

  matchup(w: MyWager): string {
    const a = w.player1Name?.trim() || '—';
    const b = w.player2Name?.trim() || '—';
    return `${a} vs ${b}`;
  }

  pickLine(w: MyWager): string {
    const d = (w.pickDescription || '').trim();
    if (d) return d;
    return formatWagerPick(w.marketType, w.side, w.player1Name, w.player2Name);
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set('');
    this.wagers.set([]);

    const kindRaw = (this.route.snapshot.paramMap.get('kind') || '').toLowerCase();
    const idStr = this.route.snapshot.paramMap.get('id') || '';
    const id = parseInt(idStr, 10);
    const fromQuery = this.route.snapshot.queryParamMap.get('tournamentId');
    const tidQ = fromQuery != null && fromQuery !== '' ? parseInt(fromQuery, 10) : NaN;

    try {
      await this.adminTournament.ensureLoaded();
    } catch {
      /* use query id only */
    }
    const tidFromCtx = this.adminTournament.tournamentId();
    const tournamentId = Number.isFinite(tidQ) && tidQ > 0 ? tidQ : tidFromCtx;

    if (kindRaw !== 'player' && kindRaw !== 'game') {
      this.error.set('Invalid list type.');
      this.loading.set(false);
      return;
    }
    const kind = kindRaw as Kind;

    if (tournamentId == null || tournamentId < 1) {
      this.error.set('Missing or invalid tournament (add ?tournamentId= to the URL or select a tournament in admin).');
      this.loading.set(false);
      return;
    }
    if (!Number.isFinite(id) || id < 1) {
      this.error.set('Invalid id.');
      this.loading.set(false);
      return;
    }

    if (kind === 'player') {
      this.headTitle.set('Wagers (player)');
      this.detailLine.set(`Tournament #${tournamentId} — player #${id}`);
    } else {
      this.headTitle.set('Wagers (game)');
      this.detailLine.set(`Tournament #${tournamentId} — game #${id}`);
    }

    try {
      const list =
        kind === 'player'
          ? await this.adminApi.getWagersForPlayerTournament(tournamentId, id)
          : await this.adminApi.getWagersForGameAdmin(id, tournamentId);
      this.wagers.set(list);
    } catch (e) {
      this.error.set(e instanceof Error ? e.message : 'Failed to load wagers.');
    } finally {
      this.loading.set(false);
    }
  }
}
