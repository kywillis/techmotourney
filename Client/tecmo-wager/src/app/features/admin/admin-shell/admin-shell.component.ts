import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AdminTournamentContextService } from '../../../core/services/admin-tournament-context.service';

@Component({
  selector: 'app-admin-shell',
  standalone: true,
  imports: [RouterOutlet],
  template: '<router-outlet />'
})
export class AdminShellComponent implements OnInit {
  private adminTournament = inject(AdminTournamentContextService);

  ngOnInit(): void {
    void this.adminTournament.ensureLoaded();
  }
}
