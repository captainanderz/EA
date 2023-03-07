export enum UserRole {
  SuperAdmin = 'SuperAdmin',
  Administrator = 'Administrator',
  Contributor = 'Contributor',
  Reader = 'Reader',
}

export enum UserBulkOption {
  RemoveSelected = 'RemoveSelected',
  ChangeRoles = 'ChangeRoles',
}

export const displayUserRole = (userRole: UserRole): string => {
  switch (userRole) {
    case UserRole.SuperAdmin:
      return 'Super Admin';

    case UserRole.Administrator:
      return 'Administrator';

    case UserRole.Contributor:
      return 'Contributor';

    case UserRole.Reader:
      return 'Reader';

    default:
      return userRole;
  }
};

export const displayUserBulkOption = (
  userBulkOption: UserBulkOption
): string => {
  switch (userBulkOption) {
    case UserBulkOption.RemoveSelected:
      return 'Remove selected';

    case UserBulkOption.ChangeRoles:
      return 'Change roles';

    default:
      return userBulkOption;
  }
};
