import { Component, OnInit, OnDestroy, inject, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { Router } from '@angular/router';
import { StarFlankedTitleComponent } from '../../shared/components/star-flanked-title/star-flanked-title.component';
import { WagerAuthService } from '../../core/services/wager-auth.service';
import { ConfigService } from '../../core/services/config.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [StarFlankedTitleComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.less'
})
export class LoginComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('googleButton', { static: false }) googleButtonRef!: ElementRef<HTMLDivElement>;

  private router = inject(Router);
  private auth = inject(WagerAuthService);
  private config = inject(ConfigService);

  errorMessage = '';
  loading = false;

  private callback: ((response: CredentialResponse) => void) | null = null;

  ngOnInit(): void {
    this.callback = (response: CredentialResponse) => this.handleCredential(response);
  }

  ngAfterViewInit(): void {
    this.renderGoogleButton();
  }

  ngOnDestroy(): void {
    this.callback = null;
  }

  private renderGoogleButton(): void {
    const clientId = this.config.getGoogleClientId();
    if (!clientId) {
      this.errorMessage = 'Google Sign-In is not configured. Set googleClientId in environment.';
      return;
    }
    const win = window as Window;
    if (!win.google?.accounts?.id) {
      const check = setInterval(() => {
        if (win.google?.accounts?.id && this.googleButtonRef?.nativeElement) {
          clearInterval(check);
          this.initGoogleButton(clientId);
        }
      }, 100);
      setTimeout(() => clearInterval(check), 5000);
      return;
    }
    this.initGoogleButton(clientId);
  }

  private initGoogleButton(clientId: string): void {
    const el = this.googleButtonRef?.nativeElement;
    if (!el || !window.google?.accounts?.id) return;
    window.google.accounts.id.initialize({
      client_id: clientId,
      callback: (response: CredentialResponse) => {
        if (this.callback) this.callback(response);
      }
    });
    window.google.accounts.id.renderButton(el, {
      theme: 'outline',
      size: 'large',
      width: 280
    });
  }

  private async handleCredential(response: CredentialResponse): Promise<void> {
    this.errorMessage = '';
    this.loading = true;
    try {
      const result = await this.auth.authenticateWithGoogle(response.credential);
      if (result.isPending) {
        this.router.navigate(['/pending']);
      } else {
        this.router.navigate(['/']);
      }
    } catch (err: unknown) {
      this.errorMessage = err instanceof Error ? err.message : 'Sign-in failed. Try again.';
    } finally {
      this.loading = false;
    }
  }
}
