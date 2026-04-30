import { Component, inject, signal, DestroyRef, effect } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterLinkActive, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { WagerAuthService } from '../../services/wager-auth.service';
import { ActiveTournamentService } from '../../services/active-tournament.service';
import { AdminTournamentContextService } from '../../services/admin-tournament-context.service';
import { WagerAdminApiService } from '../../services/wager-admin-api.service';

@Component({
  selector: 'app-main-nav',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, DecimalPipe],
  templateUrl: './main-nav.component.html',
  styleUrl: './main-nav.component.less'
})
export class MainNavComponent {
  private router = inject(Router);
  private auth = inject(WagerAuthService);
  private destroyRef = inject(DestroyRef);
  private activeTournament = inject(ActiveTournamentService);
  readonly adminTournament = inject(AdminTournamentContextService);
  private adminApi = inject(WagerAdminApiService);

  menuOpen = signal(false);
  adminRoute = signal(false);
  /** Count of Google signups awaiting activation (admin-only poll). */
  pendingPlayersCount = signal(0);

  hasSession = this.auth.hasSession;
  isActiveUser = this.auth.isAuthenticated;
  isPendingUser = this.auth.isPending;
  isAdmin = this.auth.isAdmin;
  balance = this.auth.balance;
  fullName = this.auth.fullName;
  profilePicDisplayIndex = this.auth.profilePicDisplayIndex;

  faceClass(profilePic: number): string {
    const n = profilePic && profilePic > 0 ? profilePic : 1;
    return `player-face player-face-${n}`;
  }

  constructor() {
    effect((onCleanup) => {
      const admin = this.auth.isAdmin();
      const active = this.auth.isAuthenticated();
      if (!admin || !active) {
        this.pendingPlayersCount.set(0);
        return;
      }
      const tick = () => {
        void this.adminApi.getPendingActivations(false).then((rows) => {
          this.pendingPlayersCount.set(rows.length);
        }).catch(() => {
          /* ignore transient poll failures */
        });
      };
      tick();
      const id = window.setInterval(tick, 10_000);
      onCleanup(() => clearInterval(id));
    });

    void this.activeTournament.refresh();
    this.syncAdminContext();
    this.router.events
      .pipe(
        filter((e): e is NavigationEnd => e instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => {
        this.closeMenu();
        void this.activeTournament.refresh();
        this.syncAdminContext();
        void this.auth.refreshBalance();
      });
  }

  private syncAdminContext(): void {
    if (this.auth.isAdmin()) {
      void this.adminTournament.ensureLoaded();
    }
  }

  onAdminTournamentChange(event: Event): void {
    const v = (event.target as HTMLSelectElement).value;
    const id = parseInt(v, 10);
    if (Number.isFinite(id) && id > 0) {
      this.adminTournament.selectTournament(id);
    }
  }

  toggleMenu(): void {
    this.menuOpen.update((v) => !v);
  }

  closeMenu(): void {
    this.menuOpen.set(false);
  }

  logout(): void {
    this.closeMenu();
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
