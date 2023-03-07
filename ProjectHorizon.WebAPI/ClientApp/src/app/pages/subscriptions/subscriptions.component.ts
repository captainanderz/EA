import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router, Params, Event, NavigationEnd } from '@angular/router';
import { allowedNodeEnvironmentFlags } from 'process';
import { BehaviorSubject, Subject } from 'rxjs';
import { debounceTime, filter, switchMap, takeUntil } from 'rxjs/operators';
import { PagedResult } from 'src/app/dtos/paged-result.model';
import { SubscriptionDto } from 'src/app/dtos/subscription-dto.model';
import { SubscriptionService } from 'src/app/services/subscription.service';

@Component({
  selector: 'app-subscription',
  templateUrl: './subscriptions.component.html'
})
export class SubscriptionsComponent implements OnInit, OnDestroy {

  subscriptions: ReadonlyArray<SubscriptionDto>;

  allItemsCount: number;
  pageSize = 20;
  pgNr = 1;

  searchTerm = '';

  private readonly behaviorSubject$ = new BehaviorSubject<{}>({});
  private readonly unsubscribe$ = new Subject<void>();
  
  constructor(public subscriptionService: SubscriptionService,
    private readonly router: Router,
    private readonly activatedRoute: ActivatedRoute) { }
  
  ngOnInit(): void {
    this.activatedRoute.queryParams.subscribe((params: Params) => {
      if (params.hasOwnProperty('pageNumber')) this.pgNr = params['pageNumber'];
      else this.pgNr = 1;

      if (params.hasOwnProperty('searchTerm'))
        this.searchTerm = params['searchTerm'];
      else this.searchTerm = '';

      this.update();
    });

    this.behaviorSubject$
    .pipe(
      debounceTime(100),
      switchMap(() =>
        this.subscriptionService.getSubscriptionsPaged(
          this.pgNr,
          this.pageSize,
          this.searchTerm
        )
      )
    )
    .subscribe((subscriptionsPaged: PagedResult<SubscriptionDto>) => {
      this.allItemsCount = subscriptionsPaged.allItemsCount;
      this.subscriptions = subscriptionsPaged.pageItems;
    });

    this.router.events
      .pipe(
        takeUntil(this.unsubscribe$),
        filter(
          (event: Event): event is NavigationEnd =>
            event instanceof NavigationEnd
        )
      )
      .subscribe((_) => this.update());

    this.update();
  }
  ngOnDestroy() {
    this.behaviorSubject$.complete();
    this.unsubscribe$.complete();
  }

  update() {
    this.behaviorSubject$.next({});
  }

  pageChange() {
    const queryParams: Params = {
      pageNumber: this.pgNr,
    };

    if (this.searchTerm.trim() !== '') queryParams.searchTerm = this.searchTerm;

    this.router.navigate([], {
      relativeTo: this.activatedRoute,
      queryParams,
    });
  }
}
