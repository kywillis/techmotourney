import { Component, Input, OnInit, Output, EventEmitter  } from '@angular/core';
import { IGameResult } from 'src/app/core/models/gameResult.model';
import { GameStatus } from 'src/app/enums';

@Component({
  selector: 'app-view-game-result',
  templateUrl: './view-game-result.component.html',
  styleUrl: './view-game-result.component.less'
})
export class ViewGameResultComponent implements OnInit {
  @Input() gameResult?: IGameResult;

  @Output() editGame = new EventEmitter<IGameResult>();
  @Output() deleteGame = new EventEmitter<IGameResult>();

  GameStatus = GameStatus;

  constructor() {}

  ngOnInit(): void {}

  editGameResult(gameResult: IGameResult) {
    this.editGame.emit(gameResult);
  }

  deleteGameResult(gameResult: IGameResult) {
    this.deleteGame.emit(gameResult);
  }
}
