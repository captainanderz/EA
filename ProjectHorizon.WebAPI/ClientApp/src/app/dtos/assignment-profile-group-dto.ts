export enum GroupMode {
  Included = '1',
  Excluded = '2',
  AllUsers = '3',
  AllDevices = '4',
}

export class AssignmentProfileGroupDto {
  azureGroupId: string | null;
  displayName: string;
  assignmentTypeId: number;
  endUserNotificationId: number;
  deliveryOptimizationPriorityId: number;
  groupModeId: number;

  constructor(temporary: TemporaryAssignmentProfileGroupDto) {
    this.azureGroupId = temporary.azureGroupId;
    this.displayName = temporary.displayName;
    this.deliveryOptimizationPriorityId = parseInt(
      temporary.deliveryOptimizationPriorityId,
      10
    );
    this.endUserNotificationId = parseInt(temporary.endUserNotificationId, 10);
    this.assignmentTypeId = temporary.assignmentTypeId;
    this.groupModeId = parseInt(temporary.groupModeId, 10);
  }
}

export class AssignmentProfileGroupDetailsDto extends AssignmentProfileGroupDto {}

export class TemporaryAssignmentProfileGroupDto {
  azureGroupId: string | null;
  displayName: string;
  assignmentTypeId: number;
  endUserNotificationId: string;
  deliveryOptimizationPriorityId: string;
  groupModeId: string;

  constructor(assignment: AssignmentProfileGroupDetailsDto) {
    this.azureGroupId = assignment.azureGroupId;
    this.displayName = assignment.displayName;
    this.deliveryOptimizationPriorityId =
      assignment.deliveryOptimizationPriorityId.toString();
    this.endUserNotificationId = assignment.endUserNotificationId.toString();
    this.assignmentTypeId = assignment.assignmentTypeId;
    this.groupModeId = assignment.groupModeId.toString();
  }
}
