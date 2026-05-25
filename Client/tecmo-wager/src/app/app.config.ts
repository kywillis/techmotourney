import { APP_INITIALIZER, ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { wagerAuthInterceptor } from './core/interceptors/wager-auth.interceptor';
import { WagerAuthService } from './core/services/wager-auth.service';

export function restoreWagerSessionFactory(auth: WagerAuthService) {
  return () => auth.restoreSession();
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([wagerAuthInterceptor])),
    {
      provide: APP_INITIALIZER,
      useFactory: restoreWagerSessionFactory,
      deps: [WagerAuthService],
      multi: true
    }
  ]
};
