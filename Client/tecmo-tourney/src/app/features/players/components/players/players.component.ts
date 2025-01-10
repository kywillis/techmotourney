import { Component, OnInit, ViewChild } from '@angular/core';
import { IPlayer } from 'src/app/core/models/player.model';
import { PlayersService } from 'src/app/core/services/players.service';
import { ModalComponent } from 'src/app/shared/components/modal/modal.component';
import { DeletePlayerComponent } from '../delete-player/delete-player.component';
import { EditPlayerComponent } from '../edit-player/edit-player.component';

@Component({
  selector: 'app-players',
  templateUrl: './players.component.html',
  styleUrls: ['./players.component.less']
})
export class PlayersComponent implements OnInit {

  @ViewChild('deletePlayerModal') deletePlayerModal!: ModalComponent;
  @ViewChild('editPlayerModal') editPlayerModal!: ModalComponent;
  @ViewChild('newPlayerModal') newPlayerModal!: ModalComponent;
  @ViewChild('deletePlayer') deletePlayer!: DeletePlayerComponent;
  @ViewChild('editPlayer') editPlayer!: EditPlayerComponent;
  players = [] as IPlayer[];
  loading = false;
  constructor(private playersService: PlayersService) { }

  ngOnInit(): void {
    this.loadPlayers();
  }

  loadPlayers():void{
    this.loading = false;
    this.playersService.getAllPlayers().subscribe({
      next: (player)=>{
        this.players = player;
        this.loading = false;
        },
      error: (error) => {
          console.error('Error fetching all players:', error);
          this.loading = false;
        }
    })
  }

  openDeletePlayer(player: IPlayer):void{
    this.deletePlayer.player = player;
    this.deletePlayer.resetStateOnPlayerChange(); 
    this.deletePlayerModal.open();
  }

  openEditPlayer(player: IPlayer):void{
    this.editPlayer.player = player;
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
}
