import { Component, EventEmitter, Input, Output, ViewChild, SimpleChanges } from '@angular/core';
import { IPlayer } from 'src/app/core/models/player.model';
import { PlayersService } from 'src/app/core/services/players.service';
import { MessageComponent } from 'src/app/shared/components/message/message.component';

@Component({
  selector: 'app-delete-player',
  templateUrl: './delete-player.component.html',
  styleUrl: './delete-player.component.less'
})
export class DeletePlayerComponent {
  @Output() playerDeleted: EventEmitter<void> = new EventEmitter();
  @Input() player!: IPlayer;
  @ViewChild("message") messageComponent!: MessageComponent;

  deleted = false;
  haveError = false;

  constructor(private playerService: PlayersService) {}

  ngOnInit(): void {
    this.deleted = false;
  }

  // Method called when the player is changed
  ngOnChanges(changes: SimpleChanges): void {
    console.log('change');
    if (changes['player'] && !changes['player'].firstChange) {
      this.resetStateOnPlayerChange();
    }
  }

  resetStateOnPlayerChange(): void {
    this.deleted = false;
    this.haveError = false;
    this.messageComponent.setMessage(''); // Clear any previous messages
  }

  delete(): void {
    this.playerService.deletePlayer(this.player.playerId).subscribe({
      next: (response) => {
        this.haveError = false;
        this.deleted = true;
        this.messageComponent.setMessage(`${this.player.fullName} deleted`);
        this.playerDeleted.emit();
      },
      error: (errorResponse) => {
        this.haveError = true;
        this.messageComponent.setMessage(`There was an error deleting the player: ${errorResponse.error.errorMessage}`, true);
        console.error('Error deleting player:', errorResponse.error.errorMessage);
      }
    });
  }
}
