import { AfterViewInit, Component, OnDestroy, OnInit } from '@angular/core';
import { Subject } from 'rxjs';
import { switchMap, takeUntil } from 'rxjs/operators';
import * as signalR from '@microsoft/signalr';
import { AppRoutes } from 'src/app/constants/app-routes';
import { UserRole } from 'src/app/constants/user-role';
import { ApprovalService } from 'src/app/services/approval.service';
import { UserStore } from 'src/app/services/user.store';
import { Router } from '@angular/router';
import { SignalRConstants } from 'src/app/constants/signalr-constants';
import { SignalRService } from 'src/app/services/signal-r.service';
import { ShoppingRequestsService } from 'src/app/services/shopping-services/shopping-requests.service';

declare var $: any;

@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.scss'],
})
export class SidebarComponent implements OnInit, OnDestroy, AfterViewInit {
  readonly appRoutes = AppRoutes;

  isUserAdmin: boolean;
  isUserSuperAdmin: boolean;
  approvalCount: number;
  shopRequestCount: number;

  private readonly unsubscribe$ = new Subject<void>();

  constructor(
    private readonly userStore: UserStore,
    private readonly approvalService: ApprovalService,
    private readonly router: Router,
    private readonly signalRService: SignalRService,
    private readonly shopRequestService: ShoppingRequestsService
  ) {}

  ngOnInit() {
    this.userStore
      .getLoggedInUser()
      .pipe(takeUntil(this.unsubscribe$))
      .subscribe((loggedInUser) => {
        if (loggedInUser?.userRole) {
          this.isUserAdmin =
            loggedInUser.userRole === UserRole.Administrator ||
            loggedInUser.userRole === UserRole.SuperAdmin;
          this.isUserSuperAdmin = loggedInUser.userRole === UserRole.SuperAdmin;

          if (this.signalRService.connection) {
            this.signalRService.connection.off(
              SignalRConstants.UpdateApprovalCountMessage
            );
          }

          if (this.signalRService.connection) {
            this.signalRService.connection.off(
              SignalRConstants.UpdateRequestCountMessage
            );
          }

          this.approvalService
            .getApprovalCount()
            .subscribe((count) => (this.approvalCount = count));

          this.signalRService.connection.on(
            SignalRConstants.UpdateApprovalCountMessage,
            (_) => this.loadApprovalCount()
          );

          this.shopRequestService.getShopRequestCount().subscribe((dto) => {
            this.shopRequestCount = dto.count;
          });

          this.signalRService.connection.on(
            SignalRConstants.UpdateRequestCountMessage,
            (_) => this.loadShopRequestCount()
          );
        }
      });
  }

  ngOnDestroy() {
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
  }

  ngAfterViewInit() {
    $(() => {
      if ($(window).width() < 767) {
        $('body').addClass('sidebar-toggled');

        $('.sidebar').addClass('toggled');

        $('.sidebar .collapse').collapse('hide');
      }
    });

    $('#sidebarToggle, #sidebarToggleTop').on('click', () => {
      $('body').toggleClass('sidebar-toggled');
      $('.sidebar').toggleClass('toggled');

      if ($('.sidebar').hasClass('toggled'))
        $('.sidebar .collapse').collapse('hide');
    });
  }

  isCurrentRouteOneOf(routes: ReadonlyArray<string>): boolean {
    const currentRoute = this.router.url.split('?', 1);
    return routes.some((route) => currentRoute.includes('/' + route));
  }

  private loadApprovalCount() {
    this.approvalService
      .getApprovalCount()
      .subscribe((count) => (this.approvalCount = count));
  }

  private loadShopRequestCount() {
    this.shopRequestService
      .getShopRequestCount()
      .subscribe((dto) => (this.shopRequestCount = dto.count));
  }
}
