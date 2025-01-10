import { NgModule, Optional, SkipSelf } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';

// Import your core services here
import { AuthenticationService } from './services/authentication.service';
import { ConfigService } from './services/config.service';
import { PlayersService } from './services/players.service';
import { ResultsService } from './services/results.service';
import { TournamentsService } from './services/tournaments.service';
import { GameTeamsService } from './services/gameTeams.service';

@NgModule({
  imports: [
    CommonModule,
    HttpClientModule  // Import HttpClientModule here if your services need HTTP
  ],
  providers: [
    ConfigService,
    AuthenticationService,
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
