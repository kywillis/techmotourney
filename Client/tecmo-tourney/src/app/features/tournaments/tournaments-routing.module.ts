import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ViewTournamentComponent } from './components/view-tournament/view-tournament.component';
import { TournamentsComponent } from './components/tournaments/tournaments.component';

const routes: Routes = [
  {
    path: 'tournaments/:id',
    component: ViewTournamentComponent
  },
  {
    path: '',
    component: TournamentsComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class TournamentsRoutingModule { }
