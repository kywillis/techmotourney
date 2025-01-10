import { Component, EventEmitter, Input, Output, ViewChild, SimpleChanges } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { IPlayer } from 'src/app/core/models/player.model';
import { CreatePlayerRequest } from 'src/app/core/models/request/createPlayerRequest.model';
import { PlayersService } from 'src/app/core/services/players.service';
import { MessageComponent } from 'src/app/shared/components/message/message.component';

@Component({
  selector: 'app-edit-player',
  templateUrl: './edit-player.component.html',
  styleUrl: './edit-player.component.less'
})
export class EditPlayerComponent {
  @Output() playerUpdated: EventEmitter<void> = new EventEmitter();
  @Input() player!: IPlayer;
  @ViewChild("message") messageComponent!: MessageComponent;
  playerForm: FormGroup;
  haveError = false;

  constructor(private fb: FormBuilder, private playerService: PlayersService) {
    this.playerForm = this.fb.group({
      name: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      playerId: [-1] // Hidden field for player ID
    });
  }

  populateForm(player: IPlayer): void {
    this.playerForm.patchValue({
      name: player.fullName,
      email: player.emailAddress,
      playerId: player.playerId
    });
  }

  resetStateOnPlayerChange(): void {
    this.haveError = false;
    this.playerForm.patchValue({
      name: this.player.fullName,
      email: this.player.emailAddress,
      playerId: this.player.playerId
    });
  }

  update(): void {
    const formData = this.playerForm.value;
    const playerData = {
      emailAddress: formData.email,
      fullName: formData.name,
      playerId: this.player.playerId
    } as CreatePlayerRequest;

    this.playerService.updatePlayer(this.player.playerId, playerData).subscribe({
      next: (response) => {
        this.player = response;
        this.haveError = false;
        this.messageComponent.setMessage(`${this.player.fullName} updated successfully`,);
        this.playerUpdated.emit();
      },
      error: (errorResponse) => {
        this.haveError = true;
        this.messageComponent.setMessage(`There was an error updating the player: ${errorResponse.error.errorMessage}`, true);
        console.error('Error updating player:', errorResponse.error.errorMessage);
      }
    });
  }
}
