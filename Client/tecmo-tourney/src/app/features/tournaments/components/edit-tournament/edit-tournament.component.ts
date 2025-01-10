import { Component, EventEmitter, Input, OnInit, Output, ViewChild } from '@angular/core';
import { finalize } from 'rxjs';
import { IPlayer } from 'src/app/core/models/player.model';
import { ISaveTournamentRequest } from 'src/app/core/models/request/saveTournamentRequest.model';
import { ITournament } from 'src/app/core/models/tournament.model';
import { PlayersService } from 'src/app/core/services/players.service';
import { TournamentsService } from 'src/app/core/services/tournaments.service';
import { MessageComponent } from 'src/app/shared/components/message/message.component';

@Component({
  selector: 'app-edit-tournament',
  templateUrl: './edit-tournament.component.html',
  styleUrl: './edit-tournament.component.less'
})
export class EditTournamentComponent implements OnInit {
  @ViewChild("message") messageComponent!: MessageComponent;
	@Output() tournamentUpdated: EventEmitter<void> = new EventEmitter();
  
  name = '';
  loading = false;
  saving = false;
  haveError = false;
  selectedPlayers: IPlayer[] = [];
  allPlayers: IPlayer[] = [];
  tournament? : ITournament;

  constructor(private playerService: PlayersService, private tournamentsService: TournamentsService){

  }

  ngOnInit(): void {
    this.load();
  }

  load():void{
    this.haveError = false;
    this.loading = true;
    this.saving = false;
        
    if(this.tournament){
    }

    this.playerService.getAllPlayers().subscribe({
      next: (players)=>{
        this.allPlayers = players;
        this.loading = false;
      },
      error: (error)=>{
        this.haveError = true;
        this.messageComponent.setMessage('Error loading players', true);
      },
    })
  }

  addPlayer(player: IPlayer):void{
    this.selectedPlayers.push(player);
  }

  removePlayer(player: IPlayer):void{
    this.selectedPlayers = this.selectedPlayers.filter(p => p.playerId != player.playerId);
  }

  selected(player: IPlayer): boolean{
    return this.selectedPlayers.some(p => p.playerId == player.playerId);
  }

  setTournament(tournament?: ITournament){
    this.tournament = tournament;
    this.loading = true;

    if(this.tournament){
      this.name = this.tournament.name;
      this.playerService.getPlayers(this.tournament.tournamentId)
      .pipe(
        finalize(() => {
          this.loading = false;
        })
      )
      .subscribe({
        next: (players)=>{
          this.selectedPlayers = players;
      },
        error:(error)=>{
          console.error('Error fetching all tournaments:', error);
        }
      });
    }
    else {
      this.tournament = undefined;
      this.name = '';
      this.loading = false;
    }
    
  }

  save(){
      this.saving = true;
      this.haveError = false;
      this.messageComponent.setMessage('')

      if(this.selectedPlayers.length < 3)
      {
        this.messageComponent.setMessage("select at least 3 players");
        this.haveError = true;
      }
      if(this.name == '')
      {
        this.messageComponent.setMessage("name is required")
        this.haveError = true;
      }

      if(this.haveError)
      {
        this.saving = false;
        return;
      }

      let tournament = {
        name: this.name,
        playerIds: this.selectedPlayers.map(player => player.playerId),
        tournamentId: (this.tournament) ? this.tournament.tournamentId : -1
      } as ISaveTournamentRequest;

      const method = tournament.tournamentId > 0 
                      ? this.tournamentsService.updateTournament.bind(this.tournamentsService) 
                      : this.tournamentsService.createTournament.bind(this.tournamentsService);

      method(tournament)
      .pipe(
        finalize(() => {
          this.saving = false;
        })
      )
      .subscribe({
        next: (tournament) =>{
          this.tournament = tournament;
          this.messageComponent.setMessage('Tournement Updated');
        },
        error: (error) =>{
          this.messageComponent.setMessage("error saving tournament", true);
        },

      });
  }
}

