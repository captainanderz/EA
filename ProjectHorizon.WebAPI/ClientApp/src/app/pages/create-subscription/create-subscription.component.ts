import { Component } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { InfoModalComponent } from 'src/app/components/modals/info-modal/info-modal.component';
import { AppRoutes } from 'src/app/constants/app-routes';
import { CountryList } from 'src/app/constants/country-list';
import { Patterns } from 'src/app/constants/patterns';
import { RegistrationDto } from 'src/app/dtos/registration-dto.model';
import { ApplicationInformationService } from 'src/app/services/application-information.service';
import { AuthService } from 'src/app/services/auth.service';
import { AzureAuthService } from 'src/app/services/azure-auth.service';
import { RegistrationDataService } from 'src/app/services/registration-data.service';
import { UserStore } from 'src/app/services/user.store';
import { isCompanyNameValid } from 'src/app/utility';

@Component({
  selector: 'app-create-subscription',
  templateUrl: './create-subscription.component.html',
})
export class CreateSubscriptionComponent {
  readonly patterns = Patterns;
  backButtonLink = AppRoutes.signUp;

  isCompanyNameValid = isCompanyNameValid;

  readonly countrySelectionTag = 'Select a country';
  countryList = CountryList;

  registrationDto = new RegistrationDto();
  returnUrl: string;

  askForPaymentAfterRegister: boolean = true;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly registrationDataService: RegistrationDataService,
    private readonly authService: AuthService,
    private readonly modalService: NgbModal,
    private readonly azureAuthService: AzureAuthService,
    private readonly userStore: UserStore,
    private readonly applicationInformationService: ApplicationInformationService
  ) {}

  ngOnInit() {
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';

    this.registrationDto.country = this.countrySelectionTag;
    this.registrationDataService.currentRegistrationInfo.subscribe((result) => {
      this.registrationDto = result;
      this.registrationDto.country = this.countrySelectionTag;
      if (this.registrationDto.azureRegistration)
        this.backButtonLink = AppRoutes.signUpAzure;
    });

    this.applicationInformationService.get().subscribe((info) => {
      this.askForPaymentAfterRegister = info.askForPaymentAfterRegister;
    });
  }

  submit(form: NgForm): void {
    if (
      form.value.country === this.countrySelectionTag
      // !isCompanyNameValid(this.registrationDto.companyName)
    )
      return;

    if (this.registrationDto.azureRegistration) {
      this.azureAuthService.register(this.registrationDto).subscribe(
        () => {
          this.loginWithAzure();
        },
        (error) => {
          switch (error.status) {
            case 400: {
              const modalRef = this.modalService.open(InfoModalComponent);
              modalRef.componentInstance.title = 'Could not create account!';
              modalRef.componentInstance.content1 = error.error;
              break;
            }
          }
        }
      );
    } else {
      this.authService.register(this.registrationDto).subscribe(
        (response) => {
          if (!this.askForPaymentAfterRegister) {
            this.router.navigateByUrl(this.returnUrl);
          } else {
            window.location.replace(response.dto.userInputUrl);
          }
        },
        (error) => {
          const modalRef = this.modalService.open(InfoModalComponent);
          modalRef.componentInstance.title = 'There was a problem!';
          modalRef.componentInstance.content1 = error.error.value;
        }
      );
    }
  }

  loginWithAzure() {
    this.azureAuthService.login().subscribe((response) => {
      if (!response.isSuccessful) return;
      else {
        this.userStore.setLoggedInUser(response.dto);

        if (response.dto.subscriptionId)
          this.router.navigateByUrl(this.returnUrl);
        else this.router.navigate([AppRoutes.createSubscription]);
      }
    });
  }
}
