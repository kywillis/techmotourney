import { Router } from '@angular/router';
import { Component, OnInit, ViewChild } from '@angular/core';
import { ITournament } from 'src/app/core/models/tournament.model';
import { TournamentsService } from 'src/app/core/services/tournaments.service';
import { ModalComponent } from 'src/app/shared/components/modal/modal.component';
import { EditTournamentComponent } from '../edit-tournament/edit-tournament.component';
import { DeleteTournamentComponent } from '../delete-tournament/delete-tournament.component';
import { GoogleAuthService } from 'src/app/core/services/google-auth.service';
import { TournamentStatus } from 'src/app/enums';

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
  constructor(
    private tournamentsService: TournamentsService,
    private router: Router,
    private googleAuth: GoogleAuthService
  ) {}
 
  ngOnInit(): void {
    this.loadTournaments();
  }

  loadTournaments(): void {
    this.loading = true;
    this.tournamentsService.getAllTournaments().subscribe({
      next: (tournaments) => {
        const sorted = [...tournaments].sort((a, b) => {
          const ta = new Date(a.startDate).getTime();
          const tb = new Date(b.startDate).getTime();
          return tb - ta;
        });
        this.tournaments = sorted;
        this.loading = false;
      },
      error: (error) => {
          console.error('Error fetching all tournaments:', error);
          this.loading = false;
        }
    });
  }

  goToTournament(tournamentId: number):void{
    this.router.navigate(['/tournaments', tournamentId, 'preliminaries']);
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

  loggedin(): boolean {
    return this.googleAuth.isAdminLoggedIn();
  }

  /** Maps API status (string enum name or legacy numeric) to readable enum label. */
  tournamentStatusLabel(status: TournamentStatus | string | number): string {
    const numericNames: Record<number, string> = {
      0: 'Waiting',
      1: 'Preliminaries',
      2: 'Tournament',
      3: 'Completed',
      4: 'Deleted'
    };
    if (typeof status === 'number' && numericNames[status] !== undefined) {
      return numericNames[status];
    }
    const s = String(status).trim();
    if (/^\d+$/.test(s)) {
      const n = Number(s);
      if (numericNames[n] !== undefined) return numericNames[n];
    }
    const byLower: Record<string, string> = {
      waiting: 'Waiting',
      preliminaries: 'Preliminaries',
      tournament: 'Tournament',
      completed: 'Completed',
      deleted: 'Deleted'
    };
    return byLower[s.toLowerCase()] ?? s;
  }
}
