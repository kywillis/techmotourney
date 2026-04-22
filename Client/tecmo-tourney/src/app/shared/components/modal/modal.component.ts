import { Component, EventEmitter, inject, Input, Output, TemplateRef, ViewChild } from '@angular/core';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';

/** Optional multi-part title (e.g. green status + date). When non-empty, replaces `title` text. */
export interface ModalTitleSegment {
  text: string;
  cssClass?: string;
}

@Component({
    selector: 'app-modal',
    templateUrl: './modal.component.html',
    styleUrl: './modal.component.less',
    standalone: false
})
export class ModalComponent {
    @Input() title: string = '';
    @Input() titleSegments: ModalTitleSegment[] | null = null;
    @Output() closed: EventEmitter<void> = new EventEmitter();
    @ViewChild('content') contentTemplate!: TemplateRef<any>;
    private modalService = inject(NgbModal);
    private modalRef?: NgbModalRef;
    closeResult = '';

    open() {
        this.modalRef = this.modalService.open(this.contentTemplate, { ariaLabelledBy: 'modal-basic-title' });
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
        if (this.modalRef) {
            this.modalRef.close();
        }
        this.closed.emit();
    }
}
