import { Component, OnDestroy, OnInit } from '@angular/core';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { delay, filter, pairwise, takeUntil } from 'rxjs/operators';
import { of, Subject } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { UserRole } from 'src/app/constants/user-role';
import { SignalRConstants } from 'src/app/constants/signalr-constants';
import { NotificationDto } from 'src/app/dtos/notification-dto.model';
import { SubscriptionDto } from 'src/app/dtos/subscription-dto.model';
import { UserDto } from 'src/app/dtos/user-dto.model';
import { NotificationService } from 'src/app/services/notification.service';
import * as signalR from '@microsoft/signalr';
import { UserStore } from 'src/app/services/user.store';
import { NotificationType } from 'src/app/constants/notification-type';
import { AppRoutes } from 'src/app/constants/app-routes';
import { ChangeSubscriptionModalComponent } from '../modals/change-subscription-modal/change-subscription-modal.component';
import {
  ActivatedRoute,
  NavigationEnd,
  Router,
  Event,
  Params,
} from '@angular/router';
import { getRouteFromUrl } from 'src/app/utility';
import { UserSubscriptionDto } from 'src/app/dtos/user-subscription-dto.model';
import { UserStoreKeys } from 'src/app/constants/user-store-keys';
import { SignalRService } from 'src/app/services/signal-r.service';
import { AppSettingsService } from 'src/app/services/app-settings.service';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss'],
})
export class HeaderComponent implements OnInit, OnDestroy {
  readonly userRole = UserRole;
  readonly appRoutes = AppRoutes;
  readonly pagesWithSearch = [
    AppRoutes.root,
    AppRoutes.privateRepository,
    AppRoutes.notifications,
    AppRoutes.auditLog,
    AppRoutes.subscriptions,
    AppRoutes.assignmentProfiles,
    AppRoutes.deploymentSchedules,
  ] as const;

  loggedInUser: UserDto;
  subscriptionDto: UserSubscriptionDto | undefined;
  recentNotifications: ReadonlyArray<NotificationDto> = [];
  hasNewNotifications = false;
  searchTerm = '';

  private readonly unsubscribe$ = new Subject<void>();

  constructor(
    private readonly userStore: UserStore,
    private readonly notificationService: NotificationService,
    private readonly modalService: NgbModal,
    private readonly router: Router,
    private readonly activatedRoute: ActivatedRoute,
    private readonly signalRService: SignalRService,
    public readonly appSettingsService: AppSettingsService
  ) {}

  ngOnInit() {
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

        this.subscriptionDto =
          loggedInUser.subscriptions[loggedInUser.currentSubscriptionIndex];

        if (this.signalRService.connection)
          this.signalRService.connection.off(
            SignalRConstants.UserNotificationMessage
          );

        this.getRecentNotifications();

        this.signalRService.connection.on(
          SignalRConstants.UserNotificationMessage,
          (_) => this.getRecentNotifications()
        );
      });

    this.activatedRoute.queryParams.subscribe((params: Params) => {
      if (params.hasOwnProperty('searchTerm'))
        this.searchTerm = params['searchTerm'];
      else this.searchTerm = '';
    });

    this.router.events
      .pipe(
        takeUntil(this.unsubscribe$),
        filter(
          (event: Event): event is NavigationEnd =>
            event instanceof NavigationEnd
        ),
        pairwise()
      )
      .subscribe(([previous, current]: [NavigationEnd, NavigationEnd]) => {
        const previousRoute = getRouteFromUrl(previous.url);
        const currentRoute = getRouteFromUrl(current.url);

        if (this.loggedInUser && previousRoute !== currentRoute)
          this.searchTerm = '';
      });
  }

  ngOnDestroy() {
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
  }

  isSearchVisible(): boolean {
    return this.pagesWithSearch.some(
      (route) => getRouteFromUrl(this.router.url) === '/' + route
    );
  }

  onSearch() {
    const queryParams: Params = {};

    if (this.searchTerm.trim() !== '') queryParams.searchTerm = this.searchTerm;

    this.router.navigate([], {
      relativeTo: this.activatedRoute,
      queryParams,
    });
  }

  onKeyDown(event: KeyboardEvent) {
    if (event.target instanceof HTMLInputElement && event.key === 'Enter') {
      event.preventDefault();
      this.onSearch();
    }
  }

  getRecentNotifications() {
    this.notificationService.getRecentNotifications().subscribe((result) => {
      this.recentNotifications = result;
      if (this.recentNotifications.some((n) => !n.isRead)) {
        this.hasNewNotifications = true;
      }
    });
  }

  getNotificationRoute(notification: NotificationDto): string {
    if (notification.type == NotificationType.ManualApproval) {
      return '/' + AppRoutes.approvals;
    } else if (notification.forPrivateRepository) {
      return '/' + AppRoutes.privateRepository;
    }

    return AppRoutes.root;
  }

  checkAlerts() {
    const callDelay = 1000 * 10; //10 second delay
    if (this.recentNotifications.some((n) => !n.isRead)) {
      var component = this;
      this.notificationService.markAllAsRead().subscribe((_) => {
        setTimeout(() => {
          component.getRecentNotifications();
          component.hasNewNotifications = false;
        }, callDelay);
      });
    }
  }

  reload() {
    let currentUrl = this.router.url;
    this.router.routeReuseStrategy.shouldReuseRoute = () => false;
    this.router.onSameUrlNavigation = 'reload';
    this.router.navigate([currentUrl]);
  }

  changeSubscription(newSubscriptionId: string) {
    this.userStore
      .changeCurrentSubscription(newSubscriptionId)
      .subscribe(() => {});
  }

  openSuperAdminChangeSubscription() {
    const modalRef = this.modalService.open(ChangeSubscriptionModalComponent);
    modalRef.closed.subscribe();
  }

  logout() {
    this.userStore.logout();
  }
}
