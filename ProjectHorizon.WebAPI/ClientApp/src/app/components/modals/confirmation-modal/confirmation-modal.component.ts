import { Component, EventEmitter, Input, Output } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'app-confirmation-modal',
  templateUrl: './confirmation-modal.component.html',
})
export class ConfirmationModalComponent {
  @Input() content1: string;
  @Input() content2: string;
  @Output() continue = new EventEmitter<undefined>(true);

  constructor(public activeModal: NgbActiveModal) {}

  doContinue() {
    this.activeModal.close();
    this.continue.emit();
  }
}
