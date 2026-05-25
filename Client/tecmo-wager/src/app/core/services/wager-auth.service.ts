import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ConfigService } from './config.service';
import { WagerAuthResponse } from '../models/wager-auth.model';
import { firstValueFrom } from 'rxjs';
import { isJwtExpired } from '../utils/jwt.util';

const STORAGE_KEY_ID_TOKEN = 'tecmo-wager.google-id-token';

/** Coerce API shape (camelCase + optional PascalCase) into WagerAuthResponse.profilePic. */
function normalizeWagerAuthResponse(raw: WagerAuthResponse): WagerAuthResponse {
  const r = raw as unknown as Record<string, unknown>;
  const fromPascal = r['ProfilePic'];
  const fromCamel = r['profilePic'];
  let profilePic: number | null | undefined =
    raw.profilePic ??
    (typeof fromCamel === 'number' ? fromCamel : null) ??
    (typeof fromPascal === 'number' ? fromPascal : null);

  if (typeof profilePic === 'string') {
    const p = parseInt(profilePic, 10);
    profilePic = Number.isFinite(p) ? p : null;
  }
  if (profilePic != null && (!Number.isFinite(profilePic) || profilePic < 0)) {
    profilePic = null;
  }

  return { ...raw, profilePic: profilePic ?? undefined };
}

@Injectable({
  providedIn: 'root'
})
export class WagerAuthService {
  private authState = signal<WagerAuthResponse | null>(null);
  private idToken = signal<string | null>(null);

  readonly currentAuth = this.authState.asReadonly();
  readonly isAuthenticated = computed(() => {
    const a = this.authState();
    return a?.isAuthenticated === true && !a?.isPending;
  });
  readonly isPending = computed(() => this.authState()?.isPending === true);
  readonly isAdmin = computed(() => this.authState()?.isAdmin === true);
  /** True when an ID token is in memory (logged in or restored from storage). */
  readonly hasSession = computed(() => !!this.idToken());
  readonly balance = computed(() => this.authState()?.balance ?? 0);
  readonly fullName = computed(() => this.authState()?.fullName ?? '');
  /** Active user profile sprite index (>0 when set). */
  readonly profilePic = computed(() => {
    const n = this.authState()?.profilePic;
    return n != null && n > 0 ? n : 0;
  });

  /** Sprite for header / UI when DB has no pic (same default as game-detail). */
  readonly profilePicDisplayIndex = computed(() => {
    const p = this.profilePic();
    return p > 0 ? p : 1;
  });

  constructor(
    private http: HttpClient,
    private config: ConfigService
  ) {}

  getToken(): string | null {
    return this.idToken();
  }

  /**
   * Call on app startup (APP_INITIALIZER). Restores token from localStorage,
   * skips if expired, then re-validates with the API.
   */
  async restoreSession(): Promise<void> {
    if (typeof localStorage === 'undefined') return;
    const stored = localStorage.getItem(STORAGE_KEY_ID_TOKEN);
    if (!stored?.trim()) return;
    if (isJwtExpired(stored)) {
      this.clearPersistedToken();
      return;
    }
    try {
      await this.authenticateWithGoogle(stored, { persist: false });
    } catch {
      this.clearPersistedToken();
      this.idToken.set(null);
      this.authState.set(null);
    }
  }

  async authenticateWithGoogle(
    idToken: string,
    options?: { persist?: boolean }
  ): Promise<WagerAuthResponse> {
    const url = `${this.config.getApiUrl()}/wager/auth/google`;
    const body = { idToken };
    const response = normalizeWagerAuthResponse(
      await firstValueFrom(this.http.post<WagerAuthResponse>(url, body))
    );
    this.idToken.set(idToken);
    this.authState.set(response);
    const persist = options?.persist !== false;
    if (persist) {
      this.persistToken(idToken);
    }
    return response;
  }

  logout(): void {
    this.idToken.set(null);
    this.authState.set(null);
    this.clearPersistedToken();
  }

  /** Call after fetching fresh data (e.g. balance) to update auth state. */
  updateBalance(balance: number): void {
    const current = this.authState();
    if (current) {
      this.authState.set({ ...current, balance });
    }
  }

  private persistToken(token: string): void {
    try {
      localStorage.setItem(STORAGE_KEY_ID_TOKEN, token);
    } catch {
      /* quota / private mode */
    }
  }

  private clearPersistedToken(): void {
    try {
      localStorage.removeItem(STORAGE_KEY_ID_TOKEN);
    } catch {
      /* ignore */
    }
  }
}
