export class BulkNotificationSettingsDto {
  userIds: ReadonlyArray<string>;
  isEnabled: boolean;
}

export type ReadonlyBulkNotificationSettingsDto = Readonly<BulkNotificationSettingsDto>;
