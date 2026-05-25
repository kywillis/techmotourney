import { Component, OnInit, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { StarFlankedTitleComponent } from '../../../shared/components/star-flanked-title/star-flanked-title.component';
import { WagerHistoryCardComponent } from '../../../shared/components/wager-history-card/wager-history-card.component';
import { WagerAdminApiService } from '../../../core/services/wager-admin-api.service';
import { WagerApiService } from '../../../core/services/wager-api.service';
import { WagerAuditEntry } from '../../../core/models/wager-audit-entry.model';
import { Tournament } from '../../../core/models/tournament.model';
import { MyWager } from '../../../core/models/my-wager.model';
import {
  AuditGameInfo,
  buildAuditScopeSummary,
  buildViewFromAuditEntry,
  formatWagerAuditMoney,
  indexGamesFromAdminResults,
  indexGamesFromWagers,
  indexWagersById,
  mergeGameInfoMaps,
  WagerHistoryCardView
} from '../../../core/utils/wager-audit-display.util';
import { buildAdminReturnQuery } from '../../../core/utils/admin-return-nav.util';

/** Sentinel: tournament filter = all audit rows for this player. */
export const ADMIN_AUDIT_ALL_TOURNAMENTS = 0;

@Component({
  selector: 'app-admin-player-audit',
  standalone: true,
  imports: [DecimalPipe, FormsModule, RouterLink, StarFlankedTitleComponent, WagerHistoryCardComponent],
  templateUrl: './admin-player-audit.component.html',
  styleUrl: './admin-player-audit.component.less'
})
export class AdminPlayerAuditComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private adminApi = inject(WagerAdminApiService);
  private wagerApi = inject(WagerApiService);

  readonly allTournamentsId = ADMIN_AUDIT_ALL_TOURNAMENTS;

  playerId = signal(0);
  playerName = signal('');
  globalBalance = signal<number | null>(null);
  tournaments = signal<Tournament[]>([]);
  selectedTournamentId: number = ADMIN_AUDIT_ALL_TOURNAMENTS;

  entries = signal<WagerAuditEntry[]>([]);
  wagersById = signal<Map<number, MyWager>>(new Map());
  gamesById = signal<Map<number, AuditGameInfo>>(new Map());
  summaryWins = signal(0);
  summaryLosses = signal(0);
  summaryNetAmount = signal(0);
  summaryScopeLabel = signal('');

  loading = signal(true);
  error = signal('');

  formatMoney = formatWagerAuditMoney;

  ngOnInit(): void {
    const id = parseInt(this.route.snapshot.paramMap.get('playerId') || '', 10);
    if (!Number.isFinite(id) || id < 1) {
      this.error.set('Invalid player.');
      this.loading.set(false);
      return;
    }
    this.playerId.set(id);
    void this.init();
  }

  get wagersLinkTournamentId(): number | null {
    const tid = this.selectedTournamentId;
    return tid > 0 ? tid : null;
  }

  get returnNavQuery(): Record<string, string> {
    const pid = this.playerId();
    return buildAdminReturnQuery(
      `/admin/players/${pid}`,
      this.playerName() || `Player ${pid}`
    ) as Record<string, string>;
  }

  balanceLinkQueryParams(): Record<string, string> {
    return { playerId: String(this.playerId()), ...this.returnNavQuery };
  }

  wagersLinkQueryParams(): Record<string, string> | null {
    const tid = this.wagersLinkTournamentId;
    if (tid == null) return null;
    return { tournamentId: String(tid), ...this.returnNavQuery };
  }

  async init(): Promise<void> {
    this.loading.set(true);
    this.error.set('');
    try {
      const [players, tournaments, active] = await Promise.all([
        this.adminApi.getPlayersForBalanceAdmin(),
        this.wagerApi.getTournaments(),
        this.wagerApi.getActiveTournament()
      ]);
      const pid = this.playerId();
      const p = players.find((x) => x.playerId === pid);
      this.playerName.set(p?.fullName ?? `Player ${pid}`);
      this.globalBalance.set(p?.balance ?? null);
      this.tournaments.set(tournaments);
      this.selectedTournamentId =
        active?.tournamentId && tournaments.some((t) => t.tournamentId === active.tournamentId)
          ? active.tournamentId
          : tournaments[0]?.tournamentId ?? ADMIN_AUDIT_ALL_TOURNAMENTS;
      await this.loadAudit();
    } catch (e) {
      this.error.set(e instanceof Error ? e.message : 'Failed to load player.');
    } finally {
      this.loading.set(false);
    }
  }

  async onTournamentChange(): Promise<void> {
    this.error.set('');
    await this.loadAudit();
  }

  async loadAudit(): Promise<void> {
    const pid = this.playerId();
    const tid = this.selectedTournamentId;
    const filterTid = tid > 0 ? tid : null;
    try {
      const rows = await this.adminApi.getPlayerAudit(pid, filterTid);
      this.entries.set(rows);

      const gameMaps: Map<number, AuditGameInfo>[] = [];
      if (tid > 0) {
        const [playerWagers, results] = await Promise.all([
          this.adminApi.getWagersForPlayerTournament(tid, pid),
          this.adminApi.getTournamentResults(tid)
        ]);
        this.wagersById.set(indexWagersById(playerWagers));
        gameMaps.push(indexGamesFromWagers(playerWagers), indexGamesFromAdminResults(results));
      } else {
        const tournamentIds = [
          ...new Set(
            rows
              .map((r) => r.tournamentId)
              .filter((id): id is number => id != null && id > 0)
          )
        ];
        const [resultsPerTournament, wagersPerTournament] = await Promise.all([
          Promise.all(tournamentIds.map((tournamentId) => this.adminApi.getTournamentResults(tournamentId))),
          Promise.all(
            tournamentIds.map((tournamentId) =>
              this.adminApi.getWagersForPlayerTournament(tournamentId, pid)
            )
          )
        ]);
        const allWagers = wagersPerTournament.flat();
        this.wagersById.set(indexWagersById(allWagers));
        gameMaps.push(indexGamesFromWagers(allWagers));
        for (const resultRows of resultsPerTournament) {
          gameMaps.push(indexGamesFromAdminResults(resultRows));
        }
      }
      this.gamesById.set(mergeGameInfoMaps(...gameMaps));

      if (tid > 0) {
        const summ = await this.adminApi.getPlayerTournamentSummary(pid, tid);
        this.summaryScopeLabel.set(summ.tournamentName || `Tournament ${tid}`);
        this.summaryWins.set(summ.wins);
        this.summaryLosses.set(summ.losses);
        this.summaryNetAmount.set(summ.netAmount);
      } else {
        const built = buildAuditScopeSummary(rows, 'All tournaments');
        this.summaryScopeLabel.set(built.scopeLabel);
        this.summaryWins.set(built.wins);
        this.summaryLosses.set(built.losses);
        this.summaryNetAmount.set(built.netAmount);
      }
    } catch (e) {
      this.entries.set([]);
      this.wagersById.set(new Map());
      this.gamesById.set(new Map());
      this.error.set(e instanceof Error ? e.message : 'Failed to load audit.');
    }
  }

  cardView(e: WagerAuditEntry): WagerHistoryCardView {
    return buildViewFromAuditEntry(e, this.wagersById(), this.gamesById(), 'admin');
  }
}
