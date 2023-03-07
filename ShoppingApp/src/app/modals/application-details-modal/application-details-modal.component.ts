import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { ApplicationDetailsDto } from 'src/app/dtos/application-details-dto.model';
import { ApplicationDto } from 'src/app/dtos/application-dto';
import { RequestState } from 'src/app/dtos/request-state.model';
import { ApplicationsService } from 'src/app/services/applications.service';

@Component({
  selector: 'app-application-details-modal',
  templateUrl: './application-details-modal.component.html',
  styleUrls: ['./application-details-modal.component.scss'],
})
export class ApplicationDetailsModalComponent implements OnInit {
  // Fields
  RequestState = RequestState;

  @Input()
  application: ApplicationDto;

  applicationDetails: ApplicationDetailsDto | undefined;

  @Output()
  onRequest: EventEmitter<ApplicationDto> = new EventEmitter();

  // Constructor
  constructor(
    public activeModal: NgbActiveModal,
    private service: ApplicationsService
  ) {}

  // Methods

  // Get the application details so it can render them in the modal
  ngOnInit(): void {
    this.service
      .getDetails(this.application.id, this.application.isPrivate)
      .subscribe((applicationDetails) => {
        this.applicationDetails = applicationDetails;
      });
  }

  // Handles the request button action
  request(): void {
    this.onRequest.emit(this.application);
  }
}
