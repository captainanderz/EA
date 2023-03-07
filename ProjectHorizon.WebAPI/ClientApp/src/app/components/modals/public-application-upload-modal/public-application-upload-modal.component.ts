import { Component } from '@angular/core';
import { NgbActiveModal, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { PublicApplicationDto } from 'src/app/dtos/public-application-dto.model';
import { ApplicationService } from 'src/app/services/application-services/application.service';
import { PublicApplicationService } from 'src/app/services/application-services/public-application.service';
import { ApplicationUploadModalComponent } from '../application-upload-modal/application-upload-modal.component';

@Component({
  selector: 'app-public-application-upload-modal',
  templateUrl:
    '../application-upload-modal/application-upload-modal.component.html',
  providers: [
    {
      provide: ApplicationService,
      useClass: PublicApplicationService,
    },
  ],
})
export class PublicApplicationUploadModalComponent extends ApplicationUploadModalComponent<PublicApplicationDto> {
  constructor(
    applicationService: ApplicationService<PublicApplicationDto>,
    activeModal: NgbActiveModal,
    modalService: NgbModal
  ) {
    super(applicationService, activeModal, modalService);
  }
}
