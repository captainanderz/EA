import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { NotificationDto } from '../dtos/notification-dto.model';
import { ReadonlyNotificationSettingDto } from '../dtos/notification-setting-dto.model';
import { UserNotificationSettingDto } from '../dtos/user-notification-setting-dto.model';
import { ReadonlyBulkNotificationSettingsDto } from '../dtos/bulk-notification-settings.model';
import { PagedResult } from '../dtos/paged-result.model';

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  private readonly notificationsUrl = 'api/notifications';
  private readonly notificationSettingsUrl = `${this.notificationsUrl}/settings`;

  constructor(private readonly httpClient: HttpClient) {}

  getNotificationsPaged(
    pageNumber: number,
    pageSize: number,
    searchTerm: string
  ): Observable<PagedResult<NotificationDto>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString())
      .set('searchTerm', searchTerm.trim());

    return this.httpClient.get<PagedResult<NotificationDto>>(
      this.notificationsUrl,
      {
        params,
      }
    );
  }

  getRecentNotifications(): Observable<Array<NotificationDto>> {
    return this.httpClient.get<Array<NotificationDto>>(
      `${this.notificationsUrl}/recent`
    );
  }

  markAllAsRead(): Observable<any> {
    return this.httpClient.post(
      `${this.notificationsUrl}/mark-all-as-read`,
      undefined
    );
  }

  findUsersNotificationSettingsBySubscription(): Observable<
    ReadonlyArray<UserNotificationSettingDto>
  > {
    return this.httpClient.get<ReadonlyArray<UserNotificationSettingDto>>(
      `${this.notificationSettingsUrl}`
    );
  }

  updateNotificationSetting(
    notificationSettingDto: ReadonlyNotificationSettingDto
  ): Observable<ReadonlyNotificationSettingDto> {
    return this.httpClient.patch<ReadonlyNotificationSettingDto>(
      `${this.notificationSettingsUrl}`,
      notificationSettingDto
    );
  }

  updateBulkNotificationSettings(
    bulkNotificationSettingsDto: ReadonlyBulkNotificationSettingsDto
  ): Observable<ReadonlyBulkNotificationSettingsDto> {
    return this.httpClient.put<ReadonlyBulkNotificationSettingsDto>(
      `${this.notificationSettingsUrl}`,
      bulkNotificationSettingsDto
    );
  }
}
