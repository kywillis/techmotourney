import { Component, input } from '@angular/core';

@Component({
  selector: 'app-star-flanked-title',
  standalone: true,
  templateUrl: './star-flanked-title.component.html',
  styleUrl: './star-flanked-title.component.less'
})
export class StarFlankedTitleComponent {
  readonly title = input.required<string>();
  /** `lg` = 1.25rem title (home, login, pending); `md` = 1rem (inner app pages). */
  readonly size = input<'md' | 'lg'>('md');
}
