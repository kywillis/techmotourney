import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SharedModule } from 'src/app/shared/shared.module';
import { GameStationRoutingModule } from './game-station-routing.module';
import { GameStationComponent } from './components/game-station/game-station.component';

@NgModule({
  declarations: [GameStationComponent],
  imports: [CommonModule, FormsModule, SharedModule, GameStationRoutingModule]
})
export class GameStationModule {}
