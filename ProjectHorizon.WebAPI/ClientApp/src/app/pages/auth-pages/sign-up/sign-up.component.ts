import { Component, Inject } from '@angular/core';
import { NgForm } from '@angular/forms';
import { Router } from '@angular/router';
import {
  MsalGuardConfiguration,
  MsalService,
  MSAL_GUARD_CONFIG,
} from '@azure/msal-angular';
import { InteractionType, RedirectRequest } from '@azure/msal-browser';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { InfoModalComponent } from 'src/app/components/modals/info-modal/info-modal.component';
import { TermsAndConditionsModalComponent } from 'src/app/components/modals/terms-and-conditions-modal/terms-and-conditions-modal.component';
import { AppRoutes } from 'src/app/constants/app-routes';
import { Patterns } from 'src/app/constants/patterns';
import { RegistrationDto } from 'src/app/dtos/registration-dto.model';
import { AuthService } from 'src/app/services/auth.service';
import { RegistrationDataService } from 'src/app/services/registration-data.service';
import { isPersonNameValid } from 'src/app/utility';

@Component({
  selector: 'app-sign-up',
  templateUrl: './sign-up.component.html',
})
export class SignUpComponent {
  readonly patterns = Patterns;
  readonly appRoutes = AppRoutes;
  isPersonNameValid = isPersonNameValid;

  registrationDto = new RegistrationDto();

  constructor(
    @Inject(MSAL_GUARD_CONFIG) private msalGuardConfig: MsalGuardConfiguration,
    private msalAuthService: MsalService,
    private readonly router: Router,
    private readonly registrationDataService: RegistrationDataService,
    private readonly authService: AuthService,
    private readonly modalService: NgbModal
  ) {}

  ngOnInit() {
    this.registrationDataService.currentRegistrationInfo.subscribe((result) => {
      this.registrationDto = result;
    });
  }

  submit(form: NgForm) {
    if (
      form.value.password !== form.value.repeatPassword ||
      !isPersonNameValid(this.registrationDto.firstName) ||
      !isPersonNameValid(this.registrationDto.lastName)
    )
      return;

    this.authService
      .checkEmail(this.registrationDto.email)
      .subscribe((result) => {
        if (result) {
          const modalRef = this.modalService.open(InfoModalComponent);
          modalRef.componentInstance.title = 'Could not create account!';
          modalRef.componentInstance.content1 = `An account with ${this.registrationDto.email} is already registered.`;
        } else {
          this.registrationDataService.changeRegistrationInfo(
            this.registrationDto
          );
          this.router.navigate([AppRoutes.createSubscription]);
        }
      });
  }

  azureSignUp() {
    if (this.msalGuardConfig.interactionType === InteractionType.Redirect) {
      if (this.msalGuardConfig.authRequest) {
        this.msalAuthService.loginRedirect({
          ...this.msalGuardConfig.authRequest,
        } as RedirectRequest);
      } else {
        this.msalAuthService.loginRedirect();
      }
    }
  }

  openTermsAndConditionsModal() {
    const modalRef = this.modalService.open(TermsAndConditionsModalComponent, {
      size: 'lg',
      scrollable: true,
    });
  }
}
