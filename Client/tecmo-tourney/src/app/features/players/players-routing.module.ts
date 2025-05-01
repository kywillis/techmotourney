import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ViewPlayerComponent } from './components/view-player/view-player.component';
import { PlayersComponent } from './components/players/players.component';

const routes: Routes = [ 
  {
    path: 'players/:id',
    component: ViewPlayerComponent
  },
  {
    path: '',
    component: PlayersComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class PlayersRoutingModule { }
