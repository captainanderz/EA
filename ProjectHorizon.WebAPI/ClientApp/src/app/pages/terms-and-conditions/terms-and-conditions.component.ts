import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ApplicationInformationService } from 'src/app/services/application-information.service';
import { TermsAndConditionsService } from 'src/app/services/terms-and-conditions.service';

@Component({
  selector: 'app-terms-and-conditions',
  templateUrl: './terms-and-conditions.component.html',
})
export class TermsAndConditionsComponent implements OnInit {
  termsAndConditionsText: string;
  termsVersion: string;

  constructor(
    private readonly termsAndConditionsService: TermsAndConditionsService,
    private readonly applicationInformationService: ApplicationInformationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.applicationInformationService
      .get()
      .subscribe((applicationInformation) => {
        this.termsVersion = applicationInformation.termsVersion;

        this.termsAndConditionsService
          .get(applicationInformation.termsVersion)
          .subscribe((terms) => {
            this.termsAndConditionsText = terms;
          });
      });
  }

  onAccept() {
    this.termsAndConditionsService.accept().subscribe((_) => {
      this.router.navigate(['/']);
    });
  }
}
