import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild
} from '@angular/core';
import { Router } from '@angular/router';
import { GoogleAuthService } from 'src/app/core/services/google-auth.service';
import { ConfigService } from 'src/app/core/services/config.service';

/** Google Identity Services credential callback payload. */
interface GoogleCredentialResponse {
  credential: string;
}

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.less'],
  standalone: false
})
export class LoginComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('googleButton', { static: false }) googleButtonRef!: ElementRef<HTMLDivElement>;

  errorMessage = '';
  loading = false;

  private callback: ((response: GoogleCredentialResponse) => void) | null = null;

  constructor(
    private router: Router,
    private auth: GoogleAuthService,
    private config: ConfigService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.callback = (response: GoogleCredentialResponse) => {
      void this.handleCredential(response);
    };
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
      this.errorMessage = 'Google Sign-In is not configured (googleClientId in environment).';
      this.cdr.markForCheck();
      return;
    }
    const win = window as Window & { google?: { accounts: { id: { initialize: (c: unknown) => void; renderButton: (el: HTMLElement, o: unknown) => void } } } };
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
    const win = window as Window & { google?: { accounts: { id: { initialize: (c: unknown) => void; renderButton: (el: HTMLElement, o: unknown) => void } } } };
    if (!el || !win.google?.accounts?.id) return;
    win.google.accounts.id.initialize({
      client_id: clientId,
      callback: (response: GoogleCredentialResponse) => {
        if (this.callback) this.callback(response);
      }
    });
    win.google.accounts.id.renderButton(el, {
      theme: 'outline',
      size: 'large',
      width: 280
    });
  }

  private async handleCredential(response: GoogleCredentialResponse): Promise<void> {
    this.errorMessage = '';
    this.loading = true;
    this.cdr.markForCheck();
    try {
      const result = await this.auth.authenticateWithGoogle(response.credential);
      if (result.isPending) {
        this.errorMessage =
          result.message ?? 'Your account is waiting for admin activation (same as wager sign-up).';
      } else {
        this.router.navigate(['/home']);
      }
    } catch (err: unknown) {
      this.errorMessage = err instanceof Error ? err.message : 'Sign-in failed. Try again.';
    } finally {
      this.loading = false;
      this.cdr.markForCheck();
    }
  }
}
