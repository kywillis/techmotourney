import { Component, OnInit } from '@angular/core';
import { ITournament } from 'src/app/core/models/tournament.model';
import { TournamentsService } from 'src/app/core/services/tournaments.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.less']
})
export class HomeComponent implements OnInit {

  loading = false;
  tournaments: ITournament[] = [];
  constructor(private tournamentsService: TournamentsService ) { }

  ngOnInit(): void {
    this.loading = true;
    this.tournamentsService.getAllTournament().subscribe(
      (data: ITournament[]) => {
        this.tournaments = data;
        this.loading = false;
      },
      (error) => {
        console.error('Error fetching tournaments:', error);
        this.loading = false;
      }
    );
  }

}
