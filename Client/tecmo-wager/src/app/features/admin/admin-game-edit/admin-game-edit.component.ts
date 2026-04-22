import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { StarFlankedTitleComponent } from '../../../shared/components/star-flanked-title/star-flanked-title.component';
import {
  AdminGameResultRow,
  AdminPlayerBalanceListItem,
  GameTeamOption,
  SaveGameResultRequest,
  WagerAdminApiService
} from '../../../core/services/wager-admin-api.service';
import { AdminTournamentContextService } from '../../../core/services/admin-tournament-context.service';

@Component({
  selector: 'app-admin-game-edit',
  standalone: true,
  imports: [FormsModule, RouterLink, StarFlankedTitleComponent],
  templateUrl: './admin-game-edit.component.html',
  styleUrl: './admin-game-edit.component.less'
})
export class AdminGameEditComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private adminApi = inject(WagerAdminApiService);
  private adminTournament = inject(AdminTournamentContextService);

  loading = signal(true);
  error = signal('');
  message = signal('');
  savingScores = signal(false);
  savingOdds = signal(false);

  gameResultId = 0;
  row: AdminGameResultRow | null = null;

  status: string = 'Waiting';
  gameType: string = 'Tournament';
  bracketGameId = 0;

  p1Score = 0;
  p1Pass = 0;
  p1Rush = 0;
  p1GameTeamId: number | null = null;
  p2Score = 0;
  p2Pass = 0;
  p2Rush = 0;
  p2GameTeamId: number | null = null;

  spread = 0;
  favoredPlayerId: number | null = null;
  moneyLinePlayer1: number | null = null;
  moneyLinePlayer2: number | null = null;
  overUnder: number | null = null;

  /** All players (for line edits); includes non-roster accounts that may wager. */
  tournamentPlayers = signal<AdminPlayerBalanceListItem[]>([]);
  /** TC_GameTeams — option text is TeamName. */
  gameTeams = signal<GameTeamOption[]>([]);

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set('');
    const param = this.route.snapshot.paramMap.get('gameResultId');
    const id = param ? parseInt(param, 10) : NaN;
    if (!Number.isFinite(id) || id < 1) {
      this.error.set('Invalid game.');
      this.loading.set(false);
      return;
    }
    this.gameResultId = id;
    try {
      await this.adminTournament.ensureLoaded();
      const tid = this.adminTournament.tournamentId();
      if (tid == null) {
        this.error.set('No tournament context.');
        return;
      }
      const [list, detail, roster, teams] = await Promise.all([
        this.adminApi.getTournamentResults(tid),
        this.adminApi.getGameLinesForAdmin(id),
        this.adminApi.getPlayersForBalanceAdmin(),
        this.adminApi.getGameTeams()
      ]);
      this.tournamentPlayers.set(roster);
      this.gameTeams.set(teams);
      const row = list.find((x) => x.gameResultId === id);
      if (!row) {
        this.error.set('Game not found in this tournament.');
        return;
      }
      this.row = row;
      this.status = row.status || 'Waiting';
      this.gameType = row.gameType || 'Tournament';
      this.bracketGameId = row.bracketGameId ?? 0;
      this.p1Score = row.player1.score ?? 0;
      this.p1Pass = row.player1.passingYards ?? 0;
      this.p1Rush = row.player1.rushingYards ?? 0;
      this.p2Score = row.player2.score ?? 0;
      this.p2Pass = row.player2.passingYards ?? 0;
      this.p2Rush = row.player2.rushingYards ?? 0;
      this.p1GameTeamId = normalizeGameTeamSelectId(row.player1.gameTeamId);
      this.p2GameTeamId = normalizeGameTeamSelectId(row.player2.gameTeamId);

      const o = detail.odds;
      this.spread = o?.spread ?? 0;
      this.favoredPlayerId = o?.favoredPlayerId ?? null;
      this.moneyLinePlayer1 = o?.moneyLinePlayer1 ?? null;
      this.moneyLinePlayer2 = o?.moneyLinePlayer2 ?? null;
      this.overUnder = o?.overUnder ?? null;
    } catch (e) {
      this.error.set(e instanceof Error ? e.message : 'Failed to load game.');
    } finally {
      this.loading.set(false);
    }
    if (this.row && this.route.snapshot.fragment === 'odds') {
      queueMicrotask(() =>
        requestAnimationFrame(() =>
          document.getElementById('admin-odds-section')?.scrollIntoView({ block: 'start', behavior: 'smooth' })
        )
      );
    }
  }

  /** True when both sides picked the same non-empty team (invalid for Tecmo). */
  duplicateTeamSelected(): boolean {
    const a = this.p1GameTeamId;
    const b = this.p2GameTeamId;
    return a != null && b != null && a > 0 && b > 0 && a === b;
  }

  /**
   * Options for P1: all teams except the one P2 selected (so you cannot mirror-pick),
   * but always include P1's current value so bad/legacy data stays visible.
   */
  teamOptionsForP1(): GameTeamOption[] {
    return filterTeamsForSide(this.gameTeams(), this.p1GameTeamId, this.p2GameTeamId);
  }

  /** Same for P2 vs P1's selection. */
  teamOptionsForP2(): GameTeamOption[] {
    return filterTeamsForSide(this.gameTeams(), this.p2GameTeamId, this.p1GameTeamId);
  }

  /** Spread favorite: the two sides in this game; labels use tournament roster names when listed. */
  favoredPickerOptions(): { playerId: number; fullName: string }[] {
    const r = this.row;
    if (!r) return [];
    const roster = this.tournamentPlayers();
    const label = (playerId: number, fallback: string) => {
      const hit = roster.find((p) => p.playerId === playerId);
      const n = hit?.fullName?.trim() || fallback.trim() || `Player ${playerId}`;
      return { playerId, fullName: n };
    };
    return [
      label(r.player1.playerId, r.player1.playerName || ''),
      label(r.player2.playerId, r.player2.playerName || '')
    ];
  }

  async saveScores(): Promise<void> {
    const r = this.row;
    if (!r) return;
    this.message.set('');
    this.error.set('');
    this.savingScores.set(true);
    try {
      const body: SaveGameResultRequest = {
        gameResultId: r.gameResultId,
        tournamentId: r.tournamentId,
        status: this.status,
        gameType: this.gameType,
        bracketGameId: this.bracketGameId,
        player1: {
          playerId: r.player1.playerId,
          gameTeamId: this.p1GameTeamId,
          score: this.p1Score,
          passingYards: this.p1Pass,
          rushingYards: this.p1Rush
        },
        player2: {
          playerId: r.player2.playerId,
          gameTeamId: this.p2GameTeamId,
          score: this.p2Score,
          passingYards: this.p2Pass,
          rushingYards: this.p2Rush
        }
      };
      const saveRes = await this.adminApi.saveGameResult(body);
      let msg = 'Scores saved.';
      const og = saveRes.oddsGeneration;
      if (og.attempted && !og.success) {
        msg += ' ' + (og.message || 'Odds generation failed.');
        this.error.set(msg);
      } else {
        this.message.set(msg);
      }
      await this.load();
    } catch (e) {
      this.error.set(e instanceof Error ? e.message : 'Save failed.');
    } finally {
      this.savingScores.set(false);
    }
  }

  async saveOdds(): Promise<void> {
    this.message.set('');
    this.error.set('');
    this.savingOdds.set(true);
    try {
      await this.adminApi.updateGameOdds(this.gameResultId, {
        spread: Math.round(this.spread * 10) / 10,
        favoredPlayerId: this.favoredPlayerId,
        moneyLinePlayer1:
          this.moneyLinePlayer1 == null ? null : Math.round(this.moneyLinePlayer1 * 10) / 10,
        moneyLinePlayer2:
          this.moneyLinePlayer2 == null ? null : Math.round(this.moneyLinePlayer2 * 10) / 10,
        overUnder: this.overUnder
      });
      this.message.set('Odds updated.');
    } catch (e) {
      this.error.set(e instanceof Error ? e.message : 'Odds update failed.');
    } finally {
      this.savingOdds.set(false);
    }
  }
}

/** Map stored 0 / missing FK to null for select [ngValue]. */
function normalizeGameTeamSelectId(v: number | null | undefined): number | null {
  if (v == null || !Number.isFinite(v) || v <= 0) return null;
  return v;
}

function filterTeamsForSide(
  all: GameTeamOption[],
  thisSideId: number | null,
  otherSideId: number | null
): GameTeamOption[] {
  if (otherSideId == null || otherSideId <= 0) return all;
  return all.filter(
    (t) => t.gameTeamId !== otherSideId || (thisSideId != null && t.gameTeamId === thisSideId)
  );
}
