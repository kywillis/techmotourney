import { Router } from '@angular/router';
import { Component, Input, OnInit, Output, EventEmitter  } from '@angular/core';
import { IGameResult } from 'src/app/core/models/gameResult.model';
import { GameStatus } from 'src/app/enums';
import { AuthenticationService } from 'src/app/core/services/authentication.service';

@Component({
    selector: 'app-view-game-result',
    templateUrl: './view-game-result.component.html',
    styleUrl: './view-game-result.component.less',
    standalone: false
})
export class ViewGameResultComponent implements OnInit {
  @Input() gameResult?: IGameResult;
  @Input() showControls: boolean = true;
  @Input() showStatus: boolean = true;
  @Input() showDatePlayed: boolean = false;
  @Input() playerIdSpotLight: number | null = null;

  @Output() editGame = new EventEmitter<IGameResult>();
  @Output() deleteGame = new EventEmitter<IGameResult>();

  GameStatus = GameStatus;

  constructor(private router: Router, private authenticationService: AuthenticationService) {}

  ngOnInit(): void {}

  editGameResult(gameResult: IGameResult) {
    this.editGame.emit(gameResult);
  }

  deleteGameResult(gameResult: IGameResult) {
    this.deleteGame.emit(gameResult);
  }

  showPlayer(playerId: number){
    this.router.navigate(['/players', playerId]);
  }

  spotlightWon(gameResult: IGameResult, score: number) : boolean{
    if(this.playerIdSpotLight == gameResult.player1.playerId && gameResult.player1.score == score && gameResult.player1.score > gameResult.player2.score)
      return true;

    if(this.playerIdSpotLight == gameResult.player2.playerId && gameResult.player2.score == score && gameResult.player2.score > gameResult.player1.score)
      return true;

    return false;
  }

  loggedIn():boolean{
    return this.authenticationService.isAdminLoggedIn();
  }
}
