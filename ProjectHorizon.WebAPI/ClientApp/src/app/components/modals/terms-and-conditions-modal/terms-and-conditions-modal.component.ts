import { Component, OnInit } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { ApplicationInformationService } from 'src/app/services/application-information.service';
import { TermsAndConditionsService } from 'src/app/services/terms-and-conditions.service';

@Component({
  selector: 'app-terms-and-conditions-modal',
  templateUrl: './terms-and-conditions-modal.component.html',
  styleUrls: ['./terms-and-conditions-modal.component.scss'],
})
export class TermsAndConditionsModalComponent implements OnInit {
  text: string;

  constructor(
    private readonly activeModal: NgbActiveModal,
    private readonly termsAndConditionsService: TermsAndConditionsService,
    private readonly applicationInformationService: ApplicationInformationService
  ) {}

  ngOnInit(): void {
    this.applicationInformationService
      .get()
      .subscribe((applicationInformation) => {
        this.termsAndConditionsService
          .get(applicationInformation.termsVersion)
          .subscribe((terms) => {
            this.text = terms;
          });
      });
  }

  close() {
    this.activeModal.close();
  }
}
