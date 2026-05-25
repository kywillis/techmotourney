import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { StarFlankedTitleComponent } from '../../../shared/components/star-flanked-title/star-flanked-title.component';
import { WagerApiService } from '../../../core/services/wager-api.service';
import { BettableGame } from '../../../core/models/bettable-game.model';
import { Tournament } from '../../../core/models/tournament.model';

@Component({
  selector: 'app-games-list',
  standalone: true,
  imports: [RouterLink, StarFlankedTitleComponent],
  templateUrl: './games-list.component.html',
  styleUrl: './games-list.component.less'
})
export class GamesListComponent implements OnInit, OnDestroy {
  private wagerApi = inject(WagerApiService);

  private pollTimer: ReturnType<typeof setInterval> | null = null;
  private readonly pollIntervalMs = 20_000;

  openGames: BettableGame[] = [];
  inProgressGames: BettableGame[] = [];
  completedGames: BettableGame[] = [];
  activeTournament: Tournament | null = null;
  loading = true;
  error = '';

  ngOnInit(): void {
    void this.load(true);
    this.pollTimer = setInterval(() => void this.load(false), this.pollIntervalMs);
  }

  ngOnDestroy(): void {
    if (this.pollTimer != null) {
      clearInterval(this.pollTimer);
      this.pollTimer = null;
    }
  }

  async load(showFullSpinner: boolean): Promise<void> {
    if (showFullSpinner) {
      this.loading = true;
      this.error = '';
    }
    try {
      const [board, tournament] = await Promise.all([
        this.wagerApi.getGamesBoard(),
        this.wagerApi.getActiveTournament()
      ]);
      this.openGames = board.openForBetting ?? [];
      this.inProgressGames = board.inProgress ?? [];
      this.completedGames = board.completed ?? [];
      this.activeTournament = tournament;
    } catch (e) {
      if (showFullSpinner) {
        this.error = e instanceof Error ? e.message : 'Failed to load games.';
      }
    } finally {
      if (showFullSpinner) {
        this.loading = false;
      }
    }
  }

  oddsLine(game: BettableGame): string {
    const o = game.odds;
    const mag = Math.abs(o.spread ?? 0);
    const spread1 = o.favoredPlayerId === game.player1Id ? -mag : mag;
    const parts: string[] = [];
    parts.push(`Spread ${spread1 > 0 ? '+' : ''}${spread1}`);
    if (o.overUnder != null) parts.push(`O/U ${o.overUnder}`);
    if (o.moneyLinePlayer1 != null) parts.push(`ML ${o.moneyLinePlayer1}/${o.moneyLinePlayer2 ?? ''}`);
    return parts.join(' | ');
  }

  faceClass(profilePic: number): string {
    const n = profilePic && profilePic > 0 ? profilePic : 1;
    return `player-face player-face-${n}`;
  }
}
