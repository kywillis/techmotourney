import { Component, Input } from '@angular/core';
import { ITournament } from 'src/app/core/models/tournament.model';

@Component({
  selector: 'app-delete-tournament',
  templateUrl: './delete-tournament.component.html',
  styleUrl: './delete-tournament.component.less'
})
export class DeleteTournamentComponent {
  @Input() tournament! : ITournament;

}
