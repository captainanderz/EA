import { Component } from '@angular/core';
import { NgbActiveModal, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { PrivateApplicationDto } from 'src/app/dtos/private-application-dto.model';
import { ApplicationService } from 'src/app/services/application-services/application.service';
import { PrivateApplicationService } from 'src/app/services/application-services/private-application.service';
import { ApplicationUploadModalComponent } from '../application-upload-modal/application-upload-modal.component';

@Component({
  selector: 'app-private-application-upload-modal',
  templateUrl:
    '../application-upload-modal/application-upload-modal.component.html',
  providers: [
    {
      provide: ApplicationService,
      useClass: PrivateApplicationService,
    },
  ],
})
export class PrivateApplicationUploadModalComponent extends ApplicationUploadModalComponent<PrivateApplicationDto> {
  constructor(
    applicationService: ApplicationService<PrivateApplicationDto>,
    activeModal: NgbActiveModal,
    modalService: NgbModal
  ) {
    super(applicationService, activeModal, modalService);
  }
}
