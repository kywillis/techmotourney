import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { GoogleAuthService } from 'src/app/core/services/google-auth.service';

@Component({
    selector: 'app-header',
    templateUrl: './header.component.html',
    styleUrls: ['./header.component.less'],
    standalone: false
})
export class HeaderComponent implements OnInit, OnDestroy {
  isAdmin = false;
  hasSession = false;

  private sub = new Subscription();

  constructor(
    private auth: GoogleAuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
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
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }

  signOut(): void {
    this.auth.logout();
  }
}
