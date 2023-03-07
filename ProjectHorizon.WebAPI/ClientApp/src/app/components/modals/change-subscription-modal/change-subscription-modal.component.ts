import { Component, OnDestroy, OnInit } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { BehaviorSubject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { Settings } from 'src/app/constants/settings';
import { SubscriptionDto } from 'src/app/dtos/subscription-dto.model';
import { SubscriptionService } from 'src/app/services/subscription.service';
import { UserStore } from 'src/app/services/user.store';

@Component({
  selector: 'app-change-subscription-modal',
  templateUrl: './change-subscription-modal.component.html',
  styleUrls: ['./change-subscription-modal.component.scss'],
})
export class ChangeSubscriptionModalComponent implements OnInit, OnDestroy {
  currentSubscriptionSelectedId: string | undefined = undefined;
  currentSubscriptionSelectedName: string | undefined = undefined;
  currentSubscriptionSelectedIndex: number | undefined = undefined;
  subscriptionsFiltered: ReadonlyArray<SubscriptionDto> = [];

  private readonly subscriptionNameInputted = new BehaviorSubject<string>('');

  constructor(
    private readonly subscriptionService: SubscriptionService,
    private readonly userStore: UserStore,
    private readonly activeModal: NgbActiveModal
  ) {}

  ngOnInit() {
    this.subscriptionNameInputted
      .pipe(
        // wait 300ms after each keystroke before considering the term
        debounceTime(Settings.debounceTimeMs),

        // ignore new term if same as previous term
        distinctUntilChanged(),

        // switch to new search observable each time the term changes
        switchMap((subName) =>
          this.subscriptionService.filterSubscriptionsByName(subName)
        )
      )
      .subscribe((subs) => {
        this.subscriptionsFiltered = subs;
      });
  }

  ngOnDestroy() {
    this.clearSelectedSubscription();
  }

  onInput(subName: string) {
    this.subscriptionNameInputted.next(subName);
    this.currentSubscriptionSelectedIndex = undefined;
    this.currentSubscriptionSelectedId = undefined;
  }

  onKeyDown(event: KeyboardEvent) {
    const subIndex = this.currentSubscriptionSelectedIndex;
    const subsLength = this.subscriptionsFiltered.length;

    switch (event.key) {
      case 'ArrowUp':
        this.currentSubscriptionSelectedIndex =
          subIndex === undefined || subIndex! <= 0
            ? subsLength - 1
            : subIndex - 1;
        break;
      case 'ArrowDown':
        this.currentSubscriptionSelectedIndex =
          subIndex === undefined || subIndex! >= subsLength - 1
            ? 0
            : subIndex! + 1;
        break;
      case 'Enter':
        event.preventDefault();
        if (subIndex !== undefined) this.selectSubscription(subIndex);
        break;
      case 'Escape':
        event.preventDefault();
        this.clearSelectedSubscription();
        break;
    }
  }

  selectSubscription(subscriptionIndex: number) {
    const sub = this.subscriptionsFiltered[subscriptionIndex];

    this.onInput('');
    this.currentSubscriptionSelectedName = sub.name;
    this.currentSubscriptionSelectedId = sub.id;
  }

  changeCurrentSubscription() {
    if (this.currentSubscriptionSelectedId)
      this.userStore
        .changeCurrentSubscription(this.currentSubscriptionSelectedId)
        .subscribe(() => {
          this.close();
        });
  }

  clearSelectedSubscription() {
    this.onInput('');
    this.currentSubscriptionSelectedName = undefined;
  }

  close() {
    this.clearSelectedSubscription();
    this.activeModal.close();
  }
}
