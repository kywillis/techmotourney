import { Router } from '@angular/router';
import { Component, Input, input, OnInit, ViewChild } from '@angular/core';
import { ITournament } from 'src/app/core/models/tournament.model';
import { TournamentsService } from 'src/app/core/services/tournaments.service';
import { ModalComponent } from 'src/app/shared/components/modal/modal.component';
import { EditTournamentComponent } from '../edit-tournament/edit-tournament.component';
import { DeleteTournamentComponent } from '../delete-tournament/delete-tournament.component';
import { AuthenticationService } from 'src/app/core/services/authentication.service';

@Component({
    selector: 'app-tournaments',
    templateUrl: './tournaments.component.html',
    styleUrl: './tournaments.component.less',
    standalone: false
})
export class TournamentsComponent implements OnInit{
  @ViewChild('deleteTournamentModal') deleteTournamentModal!: ModalComponent;
  @ViewChild('editTournamentModal') editTournamentModal!: ModalComponent;
  @ViewChild('editTournament') editTournament!: EditTournamentComponent;
  @ViewChild('deleteTournament') deleteTournament!: DeleteTournamentComponent;
  
  tournaments = [] as ITournament[];
  loading = false;
  password = "";
  showAdmin = false;
  loginResult = "";

  constructor(private tournamentsService: TournamentsService, private router: Router, private authenticationService: AuthenticationService) { }
 
  ngOnInit(): void {
    this.loadTournaments();
  }

  loadTournaments(): void{
    this.loading = false;
    this.tournamentsService.getAllTournaments().subscribe({
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

  goToTournament(tournamentId: number):void{
    this.router.navigate(['/tournaments', tournamentId]);
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

  toggleLogin(){
    this.showAdmin = !this.showAdmin;
    this.loginResult = "";
  }

  login(){
    this.authenticationService.loginAdmin(this.password).subscribe((result)=>{
      if(result){
        this.loginResult = "admin activated";
        this.showAdmin = false;
      }
      else 
        this.loginResult = "admin login failed";
    });
  }

  loggedin():boolean{
    return this.authenticationService.isAdminLoggedIn();
  }

  logout(){
    this.showAdmin = false;
    return this.authenticationService.logoutAdmin();
  }
}
