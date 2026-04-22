import { ChangeDetectorRef, Component, OnDestroy, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { Subscription } from 'rxjs';
import { filter } from 'rxjs';
import { GoogleAuthService } from 'src/app/core/services/google-auth.service';
import { NotificationLogService } from 'src/app/core/services/notification-log.service';
import {
  TournamentAdminMenuFlags,
  TournamentAdminNavService
} from 'src/app/core/services/tournament-admin-nav.service';

@Component({
    selector: 'app-header',
    templateUrl: './header.component.html',
    styleUrls: ['./header.component.less'],
    standalone: false
})
export class HeaderComponent implements OnInit, OnDestroy {
  @ViewChild('activityLogTpl') activityLogTpl!: TemplateRef<unknown>;

  isAdmin = false;
  hasSession = false;
  /** True when the activity log has a new error entry since the modal was last opened. */
  hasUnreadError = false;
  /** Show tournament hamburger when URL is /tournaments/:id/... */
  showTournamentAdminMenu = false;
  tournamentMenuFlags: TournamentAdminMenuFlags = {
    resetEntire: false,
    restartPhase: false,
    fillFakePrelimResults: false,
    recalculateBracket: false
  };

  private sub = new Subscription();

  constructor(
    private auth: GoogleAuthService,
    private cdr: ChangeDetectorRef,
    private modal: NgbModal,
    private notificationLog: NotificationLogService,
    private router: Router,
    private tournamentAdminNav: TournamentAdminNavService
  ) {}

  get activityEntries() {
    return this.notificationLog.entries;
  }

  openActivityLog(): void {
    this.notificationLog.markActivityLogViewed();
    this.modal.open(this.activityLogTpl, { size: 'lg', scrollable: true });
  }

  ngOnInit(): void {
    this.sub.add(
      this.notificationLog.hasUnreadError$.subscribe((v) => {
        this.hasUnreadError = v;
        this.cdr.markForCheck();
      })
    );
    this.sub.add(
      this.auth.isAdminLoggedIn$.subscribe((admin) => {
        this.isAdmin = admin;
        this.cdr.markForCheck();
      })
    );
    this.sub.add(
      this.auth.hasGoogleSession$.subscribe((s) => {
        this.hasSession = s;
        this.cdr.markForCheck();
      })
    );
    this.sub.add(
      this.router.events.pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd)).subscribe(() => {
        this.updateShowTournamentAdminMenu();
      })
    );
    this.updateShowTournamentAdminMenu();
    this.sub.add(
      this.tournamentAdminNav.menuFlags$.subscribe((f) => {
        this.tournamentMenuFlags = f;
        this.cdr.markForCheck();
      })
    );
  }

  private updateShowTournamentAdminMenu(): void {
    this.showTournamentAdminMenu = /^\/tournaments\/\d+/.test(this.router.url);
  }

  onTournamentMenuResetEntire(): void {
    this.tournamentAdminNav.requestResetEntireTournament();
  }

  onTournamentMenuRestartPhase(): void {
    this.tournamentAdminNav.requestRestartToPreliminaries();
  }

  onTournamentMenuFillFakePrelimResults(): void {
    this.tournamentAdminNav.requestFillFakePrelimResults();
  }

  onTournamentMenuRecalculateBracket(): void {
    this.tournamentAdminNav.requestRecalculateBracket();
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }

  signOut(): void {
    this.auth.logout();
  }
}
