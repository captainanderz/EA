import { Component, OnDestroy, OnInit } from '@angular/core';
import {
  ActivatedRoute,
  Event,
  NavigationEnd,
  Params,
  Router,
} from '@angular/router';
import { BehaviorSubject, Subject } from 'rxjs';
import { debounceTime, filter, switchMap, takeUntil } from 'rxjs/operators';
import { displayNotificationType } from 'src/app/constants/notification-type';
import { NotificationDto } from 'src/app/dtos/notification-dto.model';
import { PagedResult } from 'src/app/dtos/paged-result.model';
import { UserDto } from 'src/app/dtos/user-dto.model';
import { NotificationService } from 'src/app/services/notification.service';
import { UserStore } from 'src/app/services/user.store';

@Component({
  selector: 'app-notifications',
  templateUrl: './notifications.component.html',
  styleUrls: ['./notifications.component.scss'],
})
export class NotificationsComponent implements OnInit, OnDestroy {
  readonly displayNotificationType = displayNotificationType;
  notifications: ReadonlyArray<NotificationDto> = [];

  pgNr = 1;
  pageSize = 20;
  allItemsCount = 0;
  searchTerm = '';

  private readonly behaviorSubject$ = new BehaviorSubject<{}>({});
  private readonly unsubscribe$ = new Subject<void>();

  constructor(
    private readonly notificationService: NotificationService,
    private readonly userStore: UserStore,
    private readonly router: Router,
    private readonly activatedRoute: ActivatedRoute
  ) {}

  ngOnInit() {
    this.activatedRoute.queryParams.subscribe((params: Params) => {
      if (params.hasOwnProperty('pageNumber')) this.pgNr = params['pageNumber'];
      else this.pgNr = 1;

      if (params.hasOwnProperty('searchTerm'))
        this.searchTerm = params['searchTerm'];
      else this.searchTerm = '';

      this.update();
    });

    this.userStore
      .getLoggedInUser()
      .pipe(
        takeUntil(this.unsubscribe$),
        filter(
          (loggedInUser: UserDto | undefined): loggedInUser is UserDto =>
            loggedInUser !== undefined
        )
      )
      .subscribe((_) => this.update());

    this.behaviorSubject$
      .pipe(
        debounceTime(100),
        switchMap(() =>
          this.notificationService.getNotificationsPaged(
            this.pgNr,
            this.pageSize,
            this.searchTerm
          )
        )
      )
      .subscribe((notificationsPaged: PagedResult<NotificationDto>) => {
        this.allItemsCount = notificationsPaged.allItemsCount;
        this.notifications = notificationsPaged.pageItems;

        if (this.notifications.some((n) => !n.isRead))
          this.notificationService.markAllAsRead().subscribe();
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
  }

  ngOnDestroy() {
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
    this.behaviorSubject$.complete();
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
