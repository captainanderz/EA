import { Component, OnDestroy, OnInit } from '@angular/core';
import { Observable, of, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import {
  displayNotificationType,
  NotificationType,
  NotificationSettingsBulkOption,
  displayNotificationSettingsBulkOption,
} from 'src/app/constants/notification-type';
import { UserRole } from 'src/app/constants/user-role';
import { UserNotificationSettingDto } from 'src/app/dtos/user-notification-setting-dto.model';
import { UserDto } from 'src/app/dtos/user-dto.model';
import { NotificationService } from 'src/app/services/notification.service';
import { UserStore } from 'src/app/services/user.store';
import { NotificationSettingDto } from 'src/app/dtos/notification-setting-dto.model';
import { BulkNotificationSettingsDto } from 'src/app/dtos/bulk-notification-settings.model';
import { MultipleSelectDirective } from '../multiple-select.directive';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-notification-settings',
  templateUrl: './notification-settings.component.html',
})
export class NotificationSettingsComponent
  extends MultipleSelectDirective<
    string,
    UserNotificationSettingDto,
    NotificationSettingsBulkOption
  >
  implements OnInit, OnDestroy
{
  readonly notificationType = NotificationType;
  readonly displayNotificationType = displayNotificationType;
  readonly displayNotificationSettingBulkOption =
    displayNotificationSettingsBulkOption;
  readonly notificationSettingBulkOptions = Object.values(
    NotificationSettingsBulkOption
  );
  readonly notificationSettingsBulkOptions = NotificationSettingsBulkOption;
  readonly userRole = UserRole;
  readonly notificationSettingDto = new NotificationSettingDto();
  readonly bulkNotificationSettingsDto = new BulkNotificationSettingsDto();

  loggedInUser: UserDto;

  private readonly unsubscribe$ = new Subject<void>();
  private readonly selectedUserIds = new Set<string>();

  constructor(
    private readonly notificationService: NotificationService,
    protected readonly userStore: UserStore,
    protected readonly router: Router,
    protected readonly activatedRoute: ActivatedRoute
  ) {
    super(userStore);
  }

  ngOnInit() {
    super.ngOnInit();
    this.userStore
      .getLoggedInUser()
      .pipe(takeUntil(this.unsubscribe$))
      .subscribe((loggedInUser) => {
        if (loggedInUser) {
          this.loggedInUser = loggedInUser;

          this.findUsersNotificationSettingsByCurrentSubscription();
        }
      });
  }

  ngOnDestroy() {
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
  }

  getAllItemIds(): Observable<string[]> {
    return of(this.pagedItems.map((item) => item.id));
  }

  private findUsersNotificationSettingsByCurrentSubscription() {
    this.notificationService
      .findUsersNotificationSettingsBySubscription()
      .subscribe((userNotificationSettings) => {
        this.pagedItems = userNotificationSettings;
        this.allItemsCount = userNotificationSettings.length;
        this.pagedItems.forEach((dto) => (dto.id = dto.userId));
      });
  }

  toggleNotificationSetting(
    indexUserNotificationSetting: number,
    notificationType: NotificationType
  ) {
    const notificationSetting = this.pagedItems[indexUserNotificationSetting];

    this.notificationSettingDto.subscriptionId =
      this.loggedInUser.subscriptionId;
    this.notificationSettingDto.applicationUserId = notificationSetting.userId;
    this.notificationSettingDto.notificationType = notificationType;

    switch (notificationType) {
      case NotificationType.NewApplication:
        this.notificationSettingDto.isEnabled =
          !notificationSetting.isNewApplication;
        break;

      case NotificationType.DeletedApplication:
        this.notificationSettingDto.isEnabled =
          !notificationSetting.isDeletedApplication;
        break;

      case NotificationType.NewVersion:
        this.notificationSettingDto.isEnabled =
          !notificationSetting.isNewVersion;
        break;

      case NotificationType.ManualApproval:
        this.notificationSettingDto.isEnabled =
          !notificationSetting.isManualApproval;
        break;

      case NotificationType.SuccessfulDeployment:
        this.notificationSettingDto.isEnabled =
          !notificationSetting.isSuccessfulDeployment;
        break;

      case NotificationType.FailedDeployment:
        this.notificationSettingDto.isEnabled =
          !notificationSetting.isFailedDeployment;
        break;

      case NotificationType.Shop:
        this.notificationSettingDto.isEnabled = !notificationSetting.isShop;
        break;

      case NotificationType.AssignmentProfiles:
        this.notificationSettingDto.isEnabled =
          !notificationSetting.isAssignmentProfiles;
        break;

      case NotificationType.DeploymentSchedules:
        this.notificationSettingDto.isEnabled =
          !notificationSetting.isDeploymentSchedules;
        break;
    }

    this.notificationService
      .updateNotificationSetting(this.notificationSettingDto)
      .subscribe((notificationSettingDto) => {
        switch (notificationSettingDto.notificationType) {
          case NotificationType.NewApplication:
            notificationSetting.isNewApplication =
              notificationSettingDto.isEnabled;
            break;

          case NotificationType.DeletedApplication:
            notificationSetting.isDeletedApplication =
              notificationSettingDto.isEnabled;
            break;

          case NotificationType.NewVersion:
            notificationSetting.isNewVersion = notificationSettingDto.isEnabled;
            break;

          case NotificationType.ManualApproval:
            notificationSetting.isManualApproval =
              notificationSettingDto.isEnabled;
            break;

          case NotificationType.SuccessfulDeployment:
            notificationSetting.isSuccessfulDeployment =
              notificationSettingDto.isEnabled;
            break;

          case NotificationType.FailedDeployment:
            notificationSetting.isFailedDeployment =
              notificationSettingDto.isEnabled;
            break;

          case NotificationType.Shop:
            notificationSetting.isShop = notificationSettingDto.isEnabled;
            break;

          case NotificationType.AssignmentProfiles:
            notificationSetting.isAssignmentProfiles =
              notificationSettingDto.isEnabled;
            break;

          case NotificationType.DeploymentSchedules:
            notificationSetting.isDeploymentSchedules =
              notificationSettingDto.isEnabled;
            break;
        }
      });
  }

  applyNotificationSettingBulkOption() {
    switch (this.selectedOption) {
      case NotificationSettingsBulkOption.EnableAll:
        this.updateBulkNotificationSettings(
          true,
          Array.from(this.selectedItemIds)
        );
        break;
      case NotificationSettingsBulkOption.DisableAll:
        this.updateBulkNotificationSettings(
          false,
          Array.from(this.selectedItemIds)
        );
        break;
    }
  }

  private updateBulkNotificationSettings(
    isEnabled: boolean,
    userIds: ReadonlyArray<string>
  ) {
    this.bulkNotificationSettingsDto.isEnabled = isEnabled;
    this.bulkNotificationSettingsDto.userIds = userIds;
    this.notificationService
      .updateBulkNotificationSettings(this.bulkNotificationSettingsDto)
      .subscribe((_) =>
        this.findUsersNotificationSettingsByCurrentSubscription()
      );
  }
}
