import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { CoreModule } from './core/core.module';
import { SharedModule } from './shared/shared.module';
import { HomeModule } from './features/home/home.module';
import { TournamentsModule } from './features/tournaments/tournaments.module';
import { PlayersModule } from './features/players/players.module';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';


@NgModule({
  declarations: [
    AppComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    CoreModule,
    SharedModule,
    HomeModule,
    TournamentsModule,
    PlayersModule,
    NgbModule,
    FormsModule
  ],
  providers: [DatePipe, provideAnimationsAsync()],
  bootstrap: [AppComponent]
})
export class AppModule { }

declare global {
  interface Window {
    bracketsViewer?: any | undefined;
  }

  interface Dataset {
    title: string;
    roster: { id: number; name: string }[];
  }
}