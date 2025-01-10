import { Component, EventEmitter, Output, ViewChild } from '@angular/core';
import { IGameResult } from 'src/app/core/models/gameResult.model';
import { ResultsService } from 'src/app/core/services/results.service';
import { MessageComponent } from 'src/app/shared/components/message/message.component';

@Component({
  selector: 'app-delete-game-result',
  templateUrl: './delete-game-result.component.html',
  styleUrl: './delete-game-result.component.less'
})
export class DeleteGameResultComponent {
  @ViewChild("message") messageComponent!: MessageComponent;
  @Output() gameDeleted: EventEmitter<void> = new EventEmitter();

  gameResult?: IGameResult;
  deleted: boolean = false;
  gameTitle: string = '';

  constructor(private resultService: ResultsService) {

  }

  setGame(gameResult: IGameResult) {
    this.messageComponent.setMessage('');
    this.deleted = false;
    let player1 = gameResult.player1;
    let player2 = gameResult.player2;
    let team1 = (gameResult.player1.teamName) ? gameResult.player1.teamName : 'not set';
    let team2 = (gameResult.player2.teamName) ? gameResult.player2.teamName : 'not set';


    this.gameTitle = `${player1.playerName} (${team1}) vs ${player2.playerName} (${team2}) `;
    this.gameResult = gameResult;
  }

  delete() {
    this.resultService.deleteResult(this.gameResult!.gameResultId).subscribe({
      next: (response) => {
        this.deleted = true;
        this.gameDeleted.emit();
        this.messageComponent.setMessage(`${this.gameTitle} deleted`);
      },
      error: (errorResponse) => {
        this.messageComponent.setMessage(`There was an error deleting the game: ${errorResponse?.error?.errorMessage}`, true);
        console.error('Error deleting game:', errorResponse.error.errorMessage);
      }
    })
  }
}
