import { Injectable } from '@angular/core';
import { HttpClient, HttpResponse } from '@angular/common/http';
import { CookieService } from 'ngx-cookie-service';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { ConfigService } from './config.service';

@Injectable({
  providedIn: 'root',
})
export class AuthenticationService {
  private apiUrl: string;
  private readonly adminAuthCookieName = 'AdminAuthToken';

  private isLoggedInSubject = new BehaviorSubject<boolean>(false);
  isAdminLoggedIn$ = this.isLoggedInSubject.asObservable();

  constructor(
    private http: HttpClient,
    private cookieService: CookieService,
    private configService: ConfigService
  ) {
    this.apiUrl = this.configService.getApiUrl() + '/admin';
    this.checkLoginStatus(); // Initial state
  }

  private checkLoginStatus(): void {
    const loggedIn = this.cookieService.check(this.adminAuthCookieName);
    this.isLoggedInSubject.next(loggedIn);
  }

  loginAdmin(password: string): Observable<boolean> {
    return this.http
      .post(
        this.apiUrl,
        { password },
        {
          observe: 'response',
          withCredentials: true,
        }
      )
      .pipe(
        tap((response: HttpResponse<any>) => {
          const success = response.status >= 200 && response.status < 300;
          if (success) {
            console.log('Admin login successful. Cookie should be set by the browser.');
            this.checkLoginStatus();
          }
        }),
        map((response) => response.status >= 200 && response.status < 300),
        catchError((error) => {
          console.error('Admin login failed', error);
          this.isLoggedInSubject.next(false);
          return of(false);
        })
      );
  }

  logoutAdmin(): void {
    this.cookieService.delete(this.adminAuthCookieName);
    this.checkLoginStatus();
  }

  getAdminAuthToken(): string | null {
    return this.cookieService.get(this.adminAuthCookieName) || null;
  }

  isAdminLoggedIn(): boolean {
    return this.isLoggedInSubject.value;
  }
}
