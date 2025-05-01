import { Component, EventEmitter, inject, Input, Output, TemplateRef, ViewChild } from '@angular/core';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';

@Component({
    selector: 'app-modal',
    templateUrl: './modal.component.html',
    styleUrl: './modal.component.less',
    standalone: false
})
export class ModalComponent {
    @Input() title: string = '';
    @Output() closed: EventEmitter<void> = new EventEmitter();
    @ViewChild('content') contentTemplate!: TemplateRef<any>;
    private modalService = inject(NgbModal);
    private modalRef?: NgbModalRef; // Store NgbModalRef here
    closeResult = '';

    constructor() { }

    open() {
        this.modalRef = this.modalService.open(this.contentTemplate, { ariaLabelledBy: 'modal-basic-title' }); // Store the NgbModalRef
        this.modalRef.result.then(
            (result) => {
                this.closeResult = `Closed with: ${result}`;
            },
            (reason) => {
                this.closeResult = `Dismissed`;
            },
        );
    }

    close() {
        console.log('closed');
        if (this.modalRef) {
            this.modalRef.close(); // Close the modal using NgbModalRef
        }
        this.closed.emit(); // Emit the closed event
    }
}