import { Component, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { PlayersService } from 'src/app/core/services/players.service';
import { ModalComponent } from 'src/app/shared/components/modal/modal.component';
import { DeletePlayerComponent } from '../delete-player/delete-player.component';
import { EditPlayerComponent } from '../edit-player/edit-player.component';
import { IPlayerSummary } from 'src/app/core/models/playerSummary.model';
import { IPlayer } from 'src/app/core/models/player.model';
import { GoogleAuthService } from 'src/app/core/services/google-auth.service';

@Component({
    selector: 'app-players',
    templateUrl: './players.component.html',
    styleUrls: ['./players.component.less'],
    standalone: false
})
export class PlayersComponent implements OnInit {

  @ViewChild('deletePlayerModal') deletePlayerModal!: ModalComponent;
  @ViewChild('editPlayerModal') editPlayerModal!: ModalComponent;
  @ViewChild('newPlayerModal') newPlayerModal!: ModalComponent;
  @ViewChild('deletePlayer') deletePlayer!: DeletePlayerComponent;
  @ViewChild('editPlayer') editPlayer!: EditPlayerComponent;
  summaries = [] as IPlayerSummary[];
  loading = false;
  constructor(
      private playersService: PlayersService, 
      private router: Router, 
      private googleAuth: GoogleAuthService) { }

  ngOnInit(): void {
    this.loadPlayers();
  }

  loadPlayers():void{
    this.loading = false;
    this.playersService.getAllPlayerSummaries().subscribe({
      next: (summaries)=>{
        this.summaries = summaries;
        this.loading = false;
        },
      error: (error) => {
          console.error('Error fetching all players:', error);
          this.loading = false;
        }
    })
  }

  openDeletePlayer(summary: IPlayerSummary):void{
    this.deletePlayer.player = summary as IPlayer;
    this.deletePlayer.resetStateOnPlayerChange(); 
    this.deletePlayerModal.open();
  }

  openEditPlayer(summary: IPlayerSummary):void{
    this.editPlayer.player = summary as IPlayer;;
    this.editPlayer.resetStateOnPlayerChange(); 
    this.editPlayerModal.open();
  }

  openNewPlayer():void{
    this.newPlayerModal.open();
  }

  newPlayerCreated():void{
		this.playerEvent('newPlayerClosed');
  }

  playerDeleted():void{
		this.playerEvent('playerDeleted');
  }

  playerUpdated():void{
		this.playerEvent('playerUpdated');
  }

  playerEvent(message: string):void{
		console.log(message)
    this.loadPlayers();

  }

  showPlayer(playerId: number){
    this.router.navigate(['/players', playerId]);
  }

  loggedIn(): boolean {
    return this.googleAuth.isAdminLoggedIn();
  }
}
