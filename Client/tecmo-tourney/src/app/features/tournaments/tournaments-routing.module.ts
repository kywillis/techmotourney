import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ViewTournamentComponent } from './components/view-tournament/view-tournament.component';
import { TournamentsComponent } from './components/tournaments/tournaments.component';

/** Paths are relative to parent `tournaments` (lazy load) → /tournaments/:id/:tab */
const routes: Routes = [
  { path: ':id/:tab', component: ViewTournamentComponent },
  { path: ':id', component: ViewTournamentComponent },
  { path: '', component: TournamentsComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class TournamentsRoutingModule { }
