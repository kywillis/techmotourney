import { Component, Input } from '@angular/core';

@Component({
    selector: 'app-section-header',
    templateUrl: './section-header.component.html',
    styleUrl: './section-header.component.less',
    standalone: false
})
export class SectionHeaderComponent {
  @Input() text: string = '';
}
