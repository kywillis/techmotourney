import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MainNavComponent } from './core/layout/main-nav/main-nav.component';
import { WagerAuthService } from './core/services/wager-auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, MainNavComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.less'
})
export class AppComponent {
  protected readonly auth = inject(WagerAuthService);
}
