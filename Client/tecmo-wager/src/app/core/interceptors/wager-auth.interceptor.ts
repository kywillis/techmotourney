import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { WagerAuthService } from '../services/wager-auth.service';

export const wagerAuthInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(WagerAuthService);
  const token = auth.getToken();
  if (token && req.url.includes('/wager/')) {
    req = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    });
  }
  return next(req);
};
