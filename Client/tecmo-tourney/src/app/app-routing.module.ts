import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

const routes: Routes = [
  { path: '', redirectTo: '/home', pathMatch: 'full' }, // Default route
  { path: 'home', loadChildren: () => import('./features/home/home.module').then(m => m.HomeModule) },
  { path: 'players', loadChildren: () => import('./features/players/players.module').then(m => m.PlayersModule) },
  { path: 'tournaments', loadChildren: () => import('./features/tournaments/tournaments.module').then(m => m.TournamentsModule) }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
