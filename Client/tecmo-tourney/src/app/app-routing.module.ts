import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

const routes: Routes = [
  { path: '', redirectTo: '/home', pathMatch: 'full' }, // Default route
  { path: 'login', loadChildren: () => import('./features/login/login.module').then(m => m.LoginModule) },
  { path: 'home', loadChildren: () => import('./features/home/home.module').then(m => m.HomeModule) },
  { path: 'players', loadChildren: () => import('./features/players/players.module').then(m => m.PlayersModule) },
  { path: 'tournaments', loadChildren: () => import('./features/tournaments/tournaments.module').then(m => m.TournamentsModule) },
  { path: 'submit-game-result', loadChildren: () => import('./features/submit-game-result/submit-game-result.module').then(m => m.SubmitGameResultModule), data: { showNavigation: true } },
  { path: 'game-station', loadChildren: () => import('./features/game-station/game-station.module').then(m => m.GameStationModule), data: { showNavigation: true } }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
