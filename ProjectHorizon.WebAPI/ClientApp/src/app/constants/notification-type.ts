export enum NotificationType {
  NewApplication = 'NewApplication',
  DeletedApplication = 'DeletedApplication',
  NewVersion = 'NewVersion',
  ManualApproval = 'ManualApproval',
  SuccessfulDeployment = 'SuccessfulDeployment',
  FailedDeployment = 'FailedDeployment',
  Shop = 'Shop',
  AssignmentProfiles = 'AssignmentProfiles',
  DeploymentSchedules = 'DeploymentSchedules',
}

export enum NotificationSettingsBulkOption {
  EnableAll = 'EnableAll',
  DisableAll = 'DisableAll',
}

export const displayNotificationType = (
  notificationType: NotificationType
): string => {
  switch (notificationType) {
    case NotificationType.NewApplication:
      return 'New application';

    case NotificationType.DeletedApplication:
      return 'Deleted application';

    case NotificationType.NewVersion:
      return 'New version';

    case NotificationType.ManualApproval:
      return 'Manual approval';

    case NotificationType.SuccessfulDeployment:
      return 'Successful deployment';

    case NotificationType.FailedDeployment:
      return 'Failed deployment';

    case NotificationType.Shop:
      return 'Shop';

    case NotificationType.AssignmentProfiles:
      return 'Assignment profiles';

    case NotificationType.DeploymentSchedules:
      return 'Deployment schedules';

    default:
      return notificationType;
  }
};

export const displayNotificationSettingsBulkOption = (
  notificationSettingBulkOption: NotificationSettingsBulkOption
) => {
  switch (notificationSettingBulkOption) {
    case NotificationSettingsBulkOption.EnableAll:
      return 'Enable all notifications';

    case NotificationSettingsBulkOption.DisableAll:
      return 'Disable all notifications';

    default:
      return notificationSettingBulkOption;
  }
};
