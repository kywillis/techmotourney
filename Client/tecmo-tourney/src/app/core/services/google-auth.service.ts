import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { firstValueFrom } from 'rxjs';
import { ConfigService } from './config.service';
import { GoogleAuthResponse } from '../models/google-auth-response.model';
import { isJwtExpired } from '../utils/jwt.util';

/**
 * Same localStorage key as tecmo-wager so one Google sign-in works on the same origin
 * (e.g. https://site.com/tourney and https://site.com/wager).
 */
const STORAGE_KEY_ID_TOKEN = 'tecmo-wager.google-id-token';

function normalizeGoogleAuthResponse(raw: GoogleAuthResponse): GoogleAuthResponse {
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
export class GoogleAuthService {
  private authState: GoogleAuthResponse | null = null;
  private idToken: string | null = null;

  private readonly isAdminSubject = new BehaviorSubject<boolean>(false);
  /** Emits when tourney admin (Google + active + IsAdmin) changes — e.g. bracket iframe. */
  readonly isAdminLoggedIn$: Observable<boolean> = this.isAdminSubject.asObservable();

  private readonly hasSessionSubject = new BehaviorSubject<boolean>(false);
  /** True when an ID token is stored (signed in or pending). */
  readonly hasGoogleSession$: Observable<boolean> = this.hasSessionSubject.asObservable();

  constructor(
    private http: HttpClient,
    private config: ConfigService
  ) {}

  getToken(): string | null {
    return this.idToken;
  }

  /** Active, non-pending user with TC_Players.IsAdmin — controls tourney admin UI. */
  isAdminLoggedIn(): boolean {
    return this.isAdminSubject.value;
  }

  /** Has Google session (including pending activation). */
  hasGoogleSession(): boolean {
    return this.hasSessionSubject.value;
  }

  getAuthState(): GoogleAuthResponse | null {
    return this.authState;
  }

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
      this.idToken = null;
      this.authState = null;
      this.refreshSubjects();
    }
  }

  async authenticateWithGoogle(
    token: string,
    options?: { persist?: boolean }
  ): Promise<GoogleAuthResponse> {
    const url = `${this.config.getApiUrl()}/wager/auth/google`;
    const body = { idToken: token };
    const response = normalizeGoogleAuthResponse(
      await firstValueFrom(this.http.post<GoogleAuthResponse>(url, body))
    );
    this.idToken = token;
    this.authState = response;
    this.refreshSubjects();
    const persist = options?.persist !== false;
    if (persist) {
      this.persistToken(token);
    }
    return response;
  }

  logout(): void {
    this.idToken = null;
    this.authState = null;
    this.clearPersistedToken();
    this.refreshSubjects();
  }

  private refreshSubjects(): void {
    const a = this.authState;
    const isAdmin =
      a?.isAuthenticated === true && a?.isPending !== true && a?.isAdmin === true;
    this.isAdminSubject.next(isAdmin);
    this.hasSessionSubject.next(!!this.idToken);
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
