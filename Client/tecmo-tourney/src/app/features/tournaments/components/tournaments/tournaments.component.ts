import { Component, OnInit, ViewChild } from '@angular/core';
import { ITournament } from 'src/app/core/models/tournament.model';
import { TournamentsService } from 'src/app/core/services/tournaments.service';
import { ModalComponent } from 'src/app/shared/components/modal/modal.component';
import { EditTournamentComponent } from '../edit-tournament/edit-tournament.component';
import { DeleteTournamentComponent } from '../delete-tournament/delete-tournament.component';

@Component({
  selector: 'app-tournaments',
  templateUrl: './tournaments.component.html',
  styleUrl: './tournaments.component.less'
})
export class TournamentsComponent implements OnInit{

  @ViewChild('deleteTournamentModal') deleteTournamentModal!: ModalComponent;
  @ViewChild('editTournamentModal') editTournamentModal!: ModalComponent;
  @ViewChild('editTournament') editTournament!: EditTournamentComponent;
  @ViewChild('deleteTournament') deleteTournament!: DeleteTournamentComponent;
  
  tournaments = [] as ITournament[];
  loading = false;

  constructor(private tournamentsService: TournamentsService) { }
 
  ngOnInit(): void {
    this.loadTournaments();
  }


  loadTournaments(): void{
    this.loading = false;
    this.tournamentsService.getAllTournament().subscribe({
      next: (tournaments) =>{
        this.tournaments = tournaments;
        this.loading = false;
      },
      error: (error) => {
          console.error('Error fetching all tournaments:', error);
          this.loading = false;
        }
    });
  }
  openNewTournament():void{
    this.editTournamentModal.title = "New Tournament";
    this.editTournament.setTournament(); 
    this.editTournamentModal.open();
  }
  openEditTournament(tournament : ITournament):void{
    this.editTournamentModal.title = "Edit Tournament";
    this.editTournament.setTournament(tournament); 
    this.editTournamentModal.open();
  }
  openDeleteTournament(tournament : ITournament):void{
    this.deleteTournament.tournament = tournament;
    this.deleteTournamentModal.open();
  }
  newTournamentCreated():void{
		this.tournamentEvent('newTournamentCreated');
  }

  tournamentDeleted():void{
		this.tournamentEvent('tournamentDeleted');
  }

  tournamentUpdated():void{
    this.loadTournaments();
		this.tournamentEvent('tournamentUpdated');
  }

  tournamentEvent(message: string):void{
		console.log(message)
    this.loadTournaments();

  }
}
