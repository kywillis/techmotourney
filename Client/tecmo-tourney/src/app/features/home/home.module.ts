import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HomeComponent } from './components/home.component';
import { TournamentsModule } from '../tournaments/tournaments.module'; 

@NgModule({
  declarations: [
    HomeComponent
  ],
  imports: [
    CommonModule,
    TournamentsModule 
  ]
})
export class HomeModule { }
