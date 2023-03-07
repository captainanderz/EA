import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ApplicationDto } from 'src/app/dtos/application-dto';
import { RequestState } from 'src/app/dtos/request-state.model';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ApplicationDetailsModalComponent } from 'src/app/modals/application-details-modal/application-details-modal.component';

@Component({
  selector: 'app-application-view',
  templateUrl: './application-view.component.html',
  styleUrls: ['./application-view.component.scss'],
})
export class ApplicationViewComponent implements OnInit {
  // Fields
  RequestState = RequestState;

  @Input()
  application: ApplicationDto;
  @Output()
  onRequest: EventEmitter<ApplicationDto> = new EventEmitter();

  // Constructor
  constructor(private readonly modalService: NgbModal) {}

  // Methods
  ngOnInit(): void {}

  // Opens the ApplicationDetails modal with information about the selected application and handles the request logic of the button
  openDetailsModal() {
    const modalRef = this.modalService.open(ApplicationDetailsModalComponent, {
      size: 'lg',
      keyboard: true,
    });

    modalRef.componentInstance.application = this.application;
    modalRef.componentInstance.onRequest.subscribe(() => {
      this.request();
    });
  }

  // Handles the request button action
  request(): void {
    this.onRequest.emit(this.application);
  }
}
