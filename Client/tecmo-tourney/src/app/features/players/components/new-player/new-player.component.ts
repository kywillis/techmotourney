import { Component, EventEmitter, OnInit, Output, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CreatePlayerRequest } from 'src/app/core/models/request/createPlayerRequest.model';
import { PlayersService } from 'src/app/core/services/players.service';
import { MessageComponent } from 'src/app/shared/components/message/message.component';

@Component({
  selector: 'app-new-player',
  templateUrl: './new-player.component.html',
  styleUrls: ['./new-player.component.less']
})
export class NewPlayerComponent implements OnInit {
	@Output() newPlayerCreated: EventEmitter<void> = new EventEmitter();
  @ViewChild("message") messageComponent!: MessageComponent;

  playerForm: FormGroup;
  haveError: boolean = false;  
  constructor(private fb: FormBuilder, private playerService: PlayersService) {
    this.playerForm = this.fb.group({
      name: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      playerId: [-1]
    });
  }

  ngOnInit(): void {
    this.playerForm.reset();
  }

  save(): void {
    if (this.playerForm.valid) {
      const formData = this.playerForm.value;
      const playerData = {
        emailAddress: formData.email,
        fullName: formData.name,
      } as CreatePlayerRequest;
  
      this.playerService.createPlayer(playerData).subscribe({
        next: (response) => {
          this.haveError = false;
          this.messageComponent.setMessage(`${response.fullName} added`);
          this.playerForm.reset();
          this.newPlayerCreated.emit();
        },
        error: (errorResponse) => {
          this.haveError = true;
          this.messageComponent.setMessage(`There was an error creating the player: ${errorResponse.error.errorMessage}`, true);
          console.error('Error saving player:', errorResponse.error.errorMessage);
        }
      });
    } else {
      this.playerForm.markAllAsTouched(); // Ensure validation errors are displayed
    }
  }
  
}
