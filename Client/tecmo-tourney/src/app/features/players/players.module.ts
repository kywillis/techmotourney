import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PlayersComponent } from './components/players/players.component';
import { NewPlayerComponent } from './components/new-player/new-player.component';
import { PlayersRoutingModule } from './players-routing.module';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { SharedModule } from 'src/app/shared/shared.module';
import { DeletePlayerComponent } from './components/delete-player/delete-player.component';
import { EditPlayerComponent } from './components/edit-player/edit-player.component';


@NgModule({
  declarations: [
    PlayersComponent,
    NewPlayerComponent,
    DeletePlayerComponent,
    EditPlayerComponent
  ],
  imports: [
    CommonModule,
    PlayersRoutingModule,
    FormsModule,
    ReactiveFormsModule ,
    SharedModule
  ]
})
export class PlayersModule { }
