import { Injectable } from '@angular/core';
import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { GoogleAuthService } from '../services/google-auth.service';

const WRITE_METHODS = new Set(['POST', 'PUT', 'PATCH', 'DELETE']);

/**
 * Sends Google ID token for mutating /tournaments API calls (required by server middleware).
 */
@Injectable()
export class GoogleAuthInterceptor implements HttpInterceptor {
  constructor(private auth: GoogleAuthService) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const token = this.auth.getToken();
    if (!token || !WRITE_METHODS.has(req.method)) {
      return next.handle(req);
    }
    if (!req.url.includes('/tournaments')) {
      return next.handle(req);
    }
    const cloned = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    });
    return next.handle(cloned);
  }
}
