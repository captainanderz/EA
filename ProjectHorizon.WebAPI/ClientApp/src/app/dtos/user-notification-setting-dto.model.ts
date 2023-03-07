import { BaseEntityId } from './base-entity-id.model';

export class UserNotificationSettingDto extends BaseEntityId<string> {
  readonly userId: string;
  readonly fullName: string;
  readonly profilePictureSmall: string;
  isNewApplication: boolean;
  isNewVersion: boolean;
  isNewAssignment: boolean;
  isSuccessfulDeployment: boolean;
  isFailedDeployment: boolean;
  isManualApproval: boolean;
  isDeletedApplication: boolean;
  isAssignmentProfiles: boolean;
  isDeploymentSchedules: boolean;
  isShop: boolean;
}

export type ReadonlyUserNotificationSettingDto =
  Readonly<UserNotificationSettingDto>;
