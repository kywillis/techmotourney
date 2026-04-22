import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { GameStationComponent } from './components/game-station/game-station.component';

const routes: Routes = [
  { path: '', pathMatch: 'full', component: GameStationComponent },
  { path: ':gameResultId', component: GameStationComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class GameStationRoutingModule {}
