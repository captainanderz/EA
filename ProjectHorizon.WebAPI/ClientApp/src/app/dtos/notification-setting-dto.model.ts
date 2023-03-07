export class NotificationSettingDto {
  applicationUserId: string;
  subscriptionId: string;
  notificationType: string;
  isEnabled: boolean;
}

export type ReadonlyNotificationSettingDto = Readonly<NotificationSettingDto>;
