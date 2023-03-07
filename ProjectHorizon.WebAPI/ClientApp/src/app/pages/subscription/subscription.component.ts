import { Component, OnDestroy, OnInit } from '@angular/core';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { ActivatedRoute, Params } from '@angular/router';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { BehaviorSubject, Subject } from 'rxjs';
import {
  debounceTime,
  filter,
  mergeMap,
  switchMap,
  takeUntil,
  tap,
} from 'rxjs/operators';
import { ChangeBillingInfoModalComponent } from 'src/app/components/modals/change-billing-info-modal/change-billing-info-modal.component';
import { ConfirmationModalComponent } from 'src/app/components/modals/confirmation-modal/confirmation-modal.component';
import { SubscriptionState } from 'src/app/constants/subscription-state';
import { BillingInfoDto } from 'src/app/dtos/billing-info-dto.model';
import { SubscriptionDetailsDto } from 'src/app/dtos/subscription-details-dto.model';
import { SubscriptionDto } from 'src/app/dtos/subscription-dto.model';
import { UserDto } from 'src/app/dtos/user-dto.model';
import { UserSubscriptionDto } from 'src/app/dtos/user-subscription-dto.model';
import { ApplicationInformationService } from 'src/app/services/application-information.service';
import { ShoppingService } from 'src/app/services/shopping-services/shopping.service';
import { SubscriptionService } from 'src/app/services/subscription.service';
import { UserService } from 'src/app/services/user.service';
import { UserStore } from 'src/app/services/user.store';

@Component({
  selector: 'app-subscription',
  templateUrl: './subscription.component.html',
  styleUrls: ['./subscription.component.scss'],
})
export class SubscriptionComponent implements OnInit, OnDestroy {
  subscriptionState = SubscriptionState;
  subscriptionDetailsDto = new SubscriptionDetailsDto();
  subscriptionId: string;
  paymentResult: string;
  addOrReplacePictureText: string;
  showDeletePicture: boolean;
  logoImage: SafeUrl | string | null = null;

  version: string;

  loggedInUser: UserDto;
  subscription: UserSubscriptionDto;

  readonly countrySelectionTag = 'Select a country';

  private readonly behaviorSubject$ = new BehaviorSubject({});
  private readonly unsubscribe$ = new Subject<void>();

  constructor(
    private readonly modalService: NgbModal,
    private readonly subscriptionService: SubscriptionService,
    private readonly applicationInformationService: ApplicationInformationService,
    private readonly activatedRoute: ActivatedRoute,
    private readonly userStore: UserStore,
    private readonly userService: UserService,
    private readonly sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void {
    this.activatedRoute.queryParams.subscribe((params: Params) => {
      if (params.hasOwnProperty('subscriptionId'))
        this.subscriptionId = params['subscriptionId'];
      else this.subscriptionId = '';

      if (params.hasOwnProperty('paymentResult'))
        this.paymentResult = params['paymentResult'];
      else this.paymentResult = '';

      if (this.subscriptionId && this.paymentResult === 'accept') {
        this.subscriptionService
          .updateLastDigits(this.subscriptionId)
          .subscribe((_) => {
            this.update();
          });
      } else {
        this.update();
      }
    });

    this.subscriptionService.getShopGroupPrefix().subscribe((dto) => {
      this.subscriptionDetailsDto.shopGroupPrefix = dto.prefix;
    });

    this.applicationInformationService
      .get()
      .subscribe((dto) => (this.version = dto.version));

    this.userStore
      .getLoggedInUser()
      .pipe(
        takeUntil(this.unsubscribe$),
        filter(
          (loggedInUser: UserDto | undefined): loggedInUser is UserDto =>
            loggedInUser !== undefined
        )
      )
      .subscribe((loggedInUser) => {
        this.loggedInUser = loggedInUser;
        this.subscription =
          loggedInUser.subscriptions[loggedInUser.currentSubscriptionIndex];
        this.update();
      });

    this.behaviorSubject$
      .pipe(
        debounceTime(100),
        switchMap(() => this.subscriptionService.getSubscriptionDetails())
      )
      .subscribe((result) => {
        this.subscriptionDetailsDto = result;

        if (this.subscriptionDetailsDto.logoSmall != '') {
          this.addOrReplacePictureText = 'Replace photo';
          this.showDeletePicture = true;
        } else {
          this.addOrReplacePictureText = 'Add photo';
          this.showDeletePicture = false;
        }

        this.loadLogo();
      });
  }

  ngOnDestroy() {
    this.behaviorSubject$.complete();
  }

  update() {
    this.behaviorSubject$.next({});
  }

  getStateDisplayName(state: string) {
    switch (state) {
      case SubscriptionState.Trial:
        return 'Free Trial';
      case SubscriptionState.Active:
        return 'Monthly Subscription';
      default:
        return state;
    }
  }

  updateShopGroupPrefix() {
    var prefix = this.subscriptionDetailsDto.shopGroupPrefix;
    this.subscriptionService.updateShopGroupPrefix(prefix).subscribe();
  }

  openBillingInformation() {
    const modalRef = this.modalService.open(ChangeBillingInfoModalComponent);

    // spread operator to make a clone of the subscriptionDetailsDto
    const billingInfo = { ...(this.subscriptionDetailsDto as BillingInfoDto) };
    if (!billingInfo.country) billingInfo.country = this.countrySelectionTag;

    modalRef.componentInstance.billingInfoDto = billingInfo;
  }

  redirectToFarPay() {
    this.subscriptionService.createNewFarpayOrder().subscribe((result) => {
      if (result.userInputUrl) {
        window.location.replace(result.userInputUrl);
      }
    });
  }

  cancelSubscription() {
    const modalRef = this.modalService.open(ConfirmationModalComponent);
    modalRef.componentInstance.content1 = `Clicking the Continue button will cancel the subscription.`;
    modalRef.componentInstance.continue.subscribe(() => {
      this.subscriptionService.cancelSubscription().subscribe(() => {
        this.update();
      });
    });
  }

  reactivateSubscription() {
    const modalRef = this.modalService.open(ConfirmationModalComponent);
    modalRef.componentInstance.content1 = `Clicking the Continue button will reactivate the subscription.`;
    modalRef.componentInstance.continue.subscribe(() => {
      this.subscriptionService.reactivateSubscription().subscribe(() => {
        this.update();
      });
    });
  }

  onLogoSelected(event: any) {
    const file: File = event.target.files[0];
    this.subscriptionService
      .changeLogo(file)
      .pipe(
        mergeMap((_) =>
          this.userService.changeCurrentSubscription(this.subscription.id)
        ),
        tap((_) => this.loadLogo())
      )
      .subscribe((u) => {
        this.userStore.setLoggedInUser(u);
        this.update();
      });
  }

  loadLogo() {
    this.subscriptionService
      .getLogo()
      .pipe(takeUntil(this.unsubscribe$))
      .subscribe(
        (blob) => {
          const objectUrl = URL.createObjectURL(blob);
          this.logoImage = this.sanitizer.bypassSecurityTrustUrl(objectUrl);
        },
        (_) => {
          this.logoImage = null;
        }
      );
  }

  deleteLogo(): void {
    const modalRef = this.modalService.open(ConfirmationModalComponent);
    modalRef.componentInstance.content1 = `This will permanently delete the subscription's logo.`;
    modalRef.componentInstance.continue.subscribe(() => {
      this.subscriptionService
        .deleteLogo()
        .pipe(
          mergeMap((_) =>
            this.userService.changeCurrentSubscription(this.subscription.id)
          ),
          tap(() => this.loadLogo())
        )
        .subscribe((u) => {
          this.userStore.setLoggedInUser(u);
          this.update();
        });
    });
  }
}
