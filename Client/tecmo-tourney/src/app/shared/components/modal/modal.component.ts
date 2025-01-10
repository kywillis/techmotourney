import { Component, ElementRef, EventEmitter, inject, Input, Output, output, TemplateRef, ViewChild } from '@angular/core';

import { ModalDismissReasons, NgbDatepickerModule, NgbModal } from '@ng-bootstrap/ng-bootstrap'

@Component({
	selector: 'app-modal',
	templateUrl: './modal.component.html',
	styleUrl: './modal.component.less'
})
export class ModalComponent {
	@Input() title: string = '';
	@Output() closed: EventEmitter<void> = new EventEmitter();
	@ViewChild('content') contentTemplate!: TemplateRef<any>;
	private modalService = inject(NgbModal);
	closeResult = '';

	constructor() { }

	open() {
		this.modalService.open(this.contentTemplate, { ariaLabelledBy: 'modal-basic-title' }).result.then(
			(result) => {
				this.closeResult = `Closed with: ${result}`;
			},
			(reason) => {
				this.closeResult = `Dismissed`;
			},
		);
	}
	close() {
		console.log('closed')
		this.closed.emit();
	}
}
