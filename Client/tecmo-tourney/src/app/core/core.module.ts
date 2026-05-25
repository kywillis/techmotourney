import { APP_INITIALIZER, NgModule, Optional, SkipSelf } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';

import { ConfigService } from './services/config.service';
import { GoogleAuthService } from './services/google-auth.service';
import { GoogleAuthInterceptor } from './interceptors/google-auth.interceptor';
import { PlayersService } from './services/players.service';
import { ResultsService } from './services/results.service';
import { TournamentsService } from './services/tournaments.service';
import { GameTeamsService } from './services/gameTeams.service';

export function initGoogleAuth(auth: GoogleAuthService): () => Promise<void> {
  return () => auth.restoreSession();
}

@NgModule({
  imports: [
    CommonModule,
    HttpClientModule  // Import HttpClientModule here if your services need HTTP
  ],
  providers: [
    ConfigService,
    GoogleAuthService,
    {
      provide: APP_INITIALIZER,
      useFactory: initGoogleAuth,
      deps: [GoogleAuthService],
      multi: true
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: GoogleAuthInterceptor,
      multi: true
    },
    PlayersService,
    ResultsService,
    TournamentsService,
    GameTeamsService
  ],
  declarations: [
  ]
})
export class CoreModule {
  constructor(@Optional() @SkipSelf() parentModule: CoreModule) {
    if (parentModule) {
      throw new Error(
        'CoreModule is already loaded. Import it in the AppModule only');
    }
  }
}
