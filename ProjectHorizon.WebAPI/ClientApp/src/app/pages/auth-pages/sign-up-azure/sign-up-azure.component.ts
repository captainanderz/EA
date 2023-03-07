import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MsalBroadcastService } from '@azure/msal-angular';
import {
  AuthenticationResult,
  EventMessage,
  EventType,
} from '@azure/msal-browser';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { Subject } from 'rxjs';
import { filter, takeUntil } from 'rxjs/operators';
import { TermsAndConditionsModalComponent } from 'src/app/components/modals/terms-and-conditions-modal/terms-and-conditions-modal.component';
import { AppRoutes } from 'src/app/constants/app-routes';
import { Patterns } from 'src/app/constants/patterns';
import { RegistrationDto } from 'src/app/dtos/registration-dto.model';
import { AzureAuthService } from 'src/app/services/azure-auth.service';
import { RegistrationDataService } from 'src/app/services/registration-data.service';
import { UserStore } from 'src/app/services/user.store';
import { isPersonNameValid } from 'src/app/utility';

@Component({
  selector: 'app-sign-up-azure',
  templateUrl: './sign-up-azure.component.html',
})
export class SignUpAzureComponent implements OnInit, OnDestroy {
  readonly azureRegistrationDto = new RegistrationDto();

  readonly patterns = Patterns;
  isPersonNameValid = isPersonNameValid;
  dataSource: any = [];

  private readonly unsubscribe$ = new Subject<void>();
  errorMessage: string;
  returnUrl: string;

  showRegister = false;

  constructor(
    private route: ActivatedRoute,
    private msalBroadcastService: MsalBroadcastService,
    private readonly azureAuthService: AzureAuthService,
    private readonly router: Router,
    private readonly userStore: UserStore,
    private readonly registrationDataService: RegistrationDataService,
    private readonly modalService: NgbModal
  ) {}

  ngOnInit() {
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';

    this.msalBroadcastService.msalSubject$
      .pipe(
        filter(
          (msg: EventMessage) =>
            msg.eventType === EventType.LOGIN_SUCCESS ||
            msg.eventType === EventType.ACQUIRE_TOKEN_SUCCESS
        ),
        takeUntil(this.unsubscribe$)
      )
      .subscribe((result: EventMessage) => {
        if (result?.payload) {
          const payload: AuthenticationResult =
            result.payload as AuthenticationResult;

          this.setAzureInfo(payload);

          this.azureAuthService.login().subscribe(
            (response) => {
              if (response.isSuccessful) {
                this.userStore.setLoggedInUser(response.dto);
                this.router.navigateByUrl(this.returnUrl);
              } else {
                this.showRegister = true;
              }
            },
            () => {
              this.router.navigate([AppRoutes.root]);
            }
          );
        }
      });
  }

  setAzureInfo(azureInfo: AuthenticationResult) {
    this.azureAuthService.setAzureAccessToken(azureInfo.accessToken);
    let fullName = azureInfo.account!.name!;
    this.azureRegistrationDto.firstName = fullName.substr(
      0,
      fullName.indexOf(' ')
    );
    this.azureRegistrationDto.lastName = fullName.substr(
      fullName.indexOf(' ') + 1
    );
    this.azureRegistrationDto.email = azureInfo.account!.username!;
  }

  openTermsAndConditionsModal() {
    const modalRef = this.modalService.open(TermsAndConditionsModalComponent, {
      size: 'lg',
      scrollable: true,
    });
  }

  submit() {
    if (
      !isPersonNameValid(this.azureRegistrationDto.firstName) ||
      !isPersonNameValid(this.azureRegistrationDto.lastName)
    )
      return;

    this.azureRegistrationDto.azureRegistration = true;
    this.registrationDataService.changeRegistrationInfo(
      this.azureRegistrationDto
    );
    this.router.navigate([AppRoutes.createSubscription]);
  }

  ngOnDestroy(): void {
    this.unsubscribe$.next(undefined);
    this.unsubscribe$.complete();
  }
}
