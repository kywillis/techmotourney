import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router'; 
import { CommonModule } from '@angular/common';
import { ViewTournamentComponent } from './components/view-tournament/view-tournament.component';
import { TournamentsRoutingModule } from './tournaments-routing.module';
import { EditTournamentComponent } from './components/edit-tournament/edit-tournament.component';
import { DeleteTournamentComponent } from './components/delete-tournament/delete-tournament.component';
import { TournamentsComponent } from './components/tournaments/tournaments.component'; // Import the missing component
import { SharedModule } from 'src/app/shared/shared.module';
import { FormsModule } from '@angular/forms'; 
import { MatTabsModule } from '@angular/material/tabs';

@NgModule({
  declarations: [
    ViewTournamentComponent,
    TournamentsComponent,    
    EditTournamentComponent,
    DeleteTournamentComponent, 
  ],
  imports: [
    CommonModule,
    TournamentsRoutingModule,
    RouterModule,
    SharedModule,
    FormsModule,
    MatTabsModule
  ]
})
export class TournamentsModule { }
