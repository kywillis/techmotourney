import { Component, EventEmitter, Input, Output } from '@angular/core';
import { IGameResult } from 'src/app/core/models/gameResult.model';

@Component({
    selector: 'app-view-game-results',
    imports: [],
    templateUrl: './view-game-results.component.html',
    styleUrl: './view-game-results.component.less'
})
export class ViewGameResultsComponent {
  @Input() gameResults: IGameResult[] = [];
  @Input() showControls: boolean = true;
  @Input() showStatus: boolean = true;
  @Input() showDatePlayed: boolean = false;
  @Input() playerIdSpotLight: number | null = null;

  @Output() editGame = new EventEmitter<IGameResult>();
  @Output() deleteGame = new EventEmitter<IGameResult>();

  editGameResult(gameResult: IGameResult) {
    this.editGame.emit(gameResult);
  }

  deleteGameResult(gameResult: IGameResult) {
    this.deleteGame.emit(gameResult);
  }
}
