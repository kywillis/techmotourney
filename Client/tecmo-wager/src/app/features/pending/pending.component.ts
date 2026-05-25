import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { Router } from '@angular/router';
import { StarFlankedTitleComponent } from '../../shared/components/star-flanked-title/star-flanked-title.component';
import { WagerAuthService } from '../../core/services/wager-auth.service';

const POLL_INTERVAL_MS = 20_000;

@Component({
  selector: 'app-pending',
  standalone: true,
  imports: [StarFlankedTitleComponent],
  templateUrl: './pending.component.html',
  styleUrl: './pending.component.less'
})
export class PendingComponent implements OnInit, OnDestroy {
  private router = inject(Router);
  private auth = inject(WagerAuthService);

  private pollTimer: ReturnType<typeof setInterval> | null = null;

  ngOnInit(): void {
    this.pollTimer = setInterval(() => this.checkActivation(), POLL_INTERVAL_MS);
  }

  ngOnDestroy(): void {
    if (this.pollTimer != null) {
      clearInterval(this.pollTimer);
      this.pollTimer = null;
    }
  }

  private async checkActivation(): Promise<void> {
    const token = this.auth.getToken();
    if (!token) {
      this.stopPolling();
      this.router.navigate(['/login']);
      return;
    }
    try {
      const result = await this.auth.authenticateWithGoogle(token);
      if (!result.isPending && result.isAuthenticated) {
        this.stopPolling();
        this.router.navigate(['/']);
      }
    } catch {
      this.stopPolling();
      this.router.navigate(['/login']);
    }
  }

  private stopPolling(): void {
    if (this.pollTimer != null) {
      clearInterval(this.pollTimer);
      this.pollTimer = null;
    }
  }
}
