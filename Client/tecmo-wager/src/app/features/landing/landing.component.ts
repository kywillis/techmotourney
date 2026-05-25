import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { StarFlankedTitleComponent } from '../../shared/components/star-flanked-title/star-flanked-title.component';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink, StarFlankedTitleComponent],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.less'
})
export class LandingComponent {}
