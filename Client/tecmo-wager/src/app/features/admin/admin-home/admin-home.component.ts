import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { StarFlankedTitleComponent } from '../../../shared/components/star-flanked-title/star-flanked-title.component';

@Component({
  selector: 'app-admin-home',
  standalone: true,
  imports: [RouterLink, StarFlankedTitleComponent],
  templateUrl: './admin-home.component.html',
  styleUrl: './admin-home.component.less'
})
export class AdminHomeComponent {}
