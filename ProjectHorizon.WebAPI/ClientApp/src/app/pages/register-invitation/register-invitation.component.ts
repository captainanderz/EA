import { Component, Inject, OnInit } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import {
  MsalGuardConfiguration,
  MsalService,
  MSAL_GUARD_CONFIG,
} from '@azure/msal-angular';
import { InteractionType, RedirectRequest } from '@azure/msal-browser';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { TermsAndConditionsModalComponent } from 'src/app/components/modals/terms-and-conditions-modal/terms-and-conditions-modal.component';
import { AppRoutes } from 'src/app/constants/app-routes';
import { Patterns } from 'src/app/constants/patterns';
import { RegisterInvitationDto } from 'src/app/dtos/register-invitation-dto';
import { InvitedUserDto } from 'src/app/dtos/register-invitation-dto copy';
import { UserService } from 'src/app/services/user.service';

@Component({
  selector: 'app-register-invitation',
  templateUrl: './register-invitation.component.html',
})
export class RegisterInvitationComponent implements OnInit {
  readonly registerInvitationDto = new RegisterInvitationDto();
  registerSubmitted = false;
  readonly patterns = Patterns;

  registrationDisplayTitle: string;
  registrationDisplayInformation: string;
  displayLoginButton = false;

  constructor(
    @Inject(MSAL_GUARD_CONFIG) private msalGuardConfig: MsalGuardConfiguration,
    private msalAuthService: MsalService,
    private readonly route: ActivatedRoute,
    private readonly modalService: NgbModal,
    private readonly userService: UserService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe((params) => {
      this.registerInvitationDto.email = params.email;
      this.registerInvitationDto.emailToken = params.token;
      this.registerInvitationDto.subscriptionName = params.subname;
      this.userService
        .isInvitedUserAlreadyRegistered({
          email: params.email,
          subscriptionName: params.subname,
        })
        .subscribe((isUserAlreadyRegistered) => {
          if (isUserAlreadyRegistered) {
            this.failedRegistration(
              `Email ${this.registerInvitationDto.email} is already registered.`
            );
          }
        });
    });
  }

  openTermsAndConditionsModal() {
    const modalRef = this.modalService.open(TermsAndConditionsModalComponent, {
      size: 'lg',
      scrollable: true,
    });
  }

  submit(form: NgForm) {
    if (form.value.password !== form.value.repeatPassword) return;
    this.userService.registerInvitation(this.registerInvitationDto).subscribe(
      (_) => {
        this.successfulRegistration();
      },
      (error) => {
        switch (error.status) {
          case 400: {
            this.failedRegistration(error.error);
            break;
          }
          case 409: {
            this.failedRegistration(error.error);
            break;
          }
          case 500: {
            this.failedRegistration('Account could not be created');
            break;
          }
        }
      }
    );
  }

  private successfulRegistration() {
    this.registerSubmitted = true;
    this.registrationDisplayTitle = 'Account successfully created';
    this.registrationDisplayInformation = `Account successfully created for email address ${this.registerInvitationDto.email}`;
    this.displayLoginButton = true;
  }

  private failedRegistration(message: string) {
    this.registerSubmitted = true;
    this.registrationDisplayTitle = 'Failed to create account';
    this.registrationDisplayInformation = message;
  }

  goToLogin() {
    this.router.navigate([AppRoutes.login]);
  }

  azureRegistration() {
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
}
