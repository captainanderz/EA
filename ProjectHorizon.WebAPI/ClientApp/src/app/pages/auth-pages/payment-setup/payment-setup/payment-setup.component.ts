import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AppRoutes } from 'src/app/constants/app-routes';
import { SubscriptionState } from 'src/app/constants/subscription-state';
import { UserStoreKeys } from 'src/app/constants/user-store-keys';
import { SubscriptionDto } from 'src/app/dtos/subscription-dto.model';
import { UserDto } from 'src/app/dtos/user-dto.model';
import { SubscriptionService } from 'src/app/services/subscription.service';
import { UserStore } from 'src/app/services/user.store';

@Component({
  selector: 'app-payment-setup',
  templateUrl: './payment-setup.component.html',
})
export class PaymentSetupComponent implements OnInit {
  subscriptionId: string;
  paymentResult: string;
  userInputUrl: string;
  paymentButtonText: string;
  showPaymentResult = false;
  userAlreadyLoggedIn = false;

  loggedInUser: UserDto;
  subscriptionDto: SubscriptionDto | undefined;

  pageTitle: string;
  pageText: string;
  pageTextExtra: string;
  pageDetails: string;

  constructor(
    private readonly activatedRoute: ActivatedRoute,
    private readonly subscriptionService: SubscriptionService,
    private readonly userStore: UserStore,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.activatedRoute.queryParams.subscribe((params: Params) => {
      if (params.hasOwnProperty('subscriptionId'))
        this.subscriptionId = params['subscriptionId'];
      else this.subscriptionId = '';

      if (params.hasOwnProperty('paymentResult'))
        this.paymentResult = params['paymentResult'];
      else this.paymentResult = '';
    });

    if (this.subscriptionId && this.paymentResult === 'accept') {
      this.subscriptionService.updateLastDigits(this.subscriptionId).subscribe(
        (result) => {
          if (result.isSuccessful && result.dto.status === 'Ok') {
            var loggedInUser = localStorage.getItem(UserStoreKeys.loggedInUser);
            if (loggedInUser) {
              this.userAlreadyLoggedIn = true;
              this.userStore.reloadCurrentSubscription();
              this.showContinue();
            } else {
              this.showAccountCreated();
            }
          } else if (result.isSuccessful && result.dto.status !== 'Ok') {
            this.userInputUrl = result.dto.userInputUrl;
            this.showPaymentFailed('Click to retry');
          }
        },
        (_) => {
          this.showPaymentFailed();
        }
      );
    } else {
      this.userStore
        .getLoggedInUser()
        .pipe(
          filter(
            (loggedInUser: UserDto | undefined): loggedInUser is UserDto =>
              loggedInUser !== undefined
          )
        )
        .subscribe((loggedInUser) => {
          this.loggedInUser = loggedInUser;

          this.subscriptionDto =
            loggedInUser.subscriptions[loggedInUser.currentSubscriptionIndex];

          if (
            this.subscriptionDto.state === SubscriptionState.PaymentNotSetUp
          ) {
            this.subscriptionService
              .getFarpayOrder(this.subscriptionDto.id)
              .subscribe((result) => {
                this.userInputUrl = result.dto.userInputUrl;
                this.showPaymentRequired();
              });
          }
        });
    }
  }

  showPaymentFailed(retryButtonTextParam?: string) {
    this.pageTitle = 'Failed to register payment';
    this.pageText =
      'Your account has been registered, but there was an issue registering your payment information.';
    if (retryButtonTextParam && retryButtonTextParam.length > 0) {
      this.paymentButtonText = retryButtonTextParam;
    }
    this.showPaymentResult = true;
  }

  showPaymentRequired() {
    this.pageTitle = 'Payment setup required';
    this.pageText = 'Click the button below to enter your payment information.';
    this.paymentButtonText = 'Enter payment';
    this.showPaymentResult = true;
  }

  showAccountCreated() {
    this.pageTitle = 'Account created';
    this.pageText = 'Your account was successfully created.';
    this.pageTextExtra =
      'We have sent a confirmation email to your email address, you will need to follow the link in the email to continue with the Sign Up process.';
    this.pageDetails = 'You might need to check your Spam / Junk email folder.';
    this.showPaymentResult = true;
  }

  showContinue() {
    this.pageTitle = 'Payment was set up';
    this.pageText = 'You can click Continue and start using your subscription.';
    this.paymentButtonText = 'Continue';
    this.showPaymentResult = true;
  }

  redirectUser() {
    if (this.userAlreadyLoggedIn) {
      this.router.navigate([AppRoutes.root]);
    } else if (this.userInputUrl) {
      window.location.replace(this.userInputUrl);
    }
  }
}
