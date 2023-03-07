import { UserRole } from '../constants/user-role';

export class BulkChangeUsersRoleDto {
  userIds: ReadonlyArray<string>;
  newUserRole: UserRole;
}

export type ReadonlyBulkChangeUsersRoleDto = Readonly<BulkChangeUsersRoleDto>;
