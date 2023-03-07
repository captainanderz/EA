import { BaseEntityIdNumber } from './base-entity-id-number.model';

export class AssignmentProfileDto extends BaseEntityIdNumber {
  name: string;
  numberOfApplicationsAssigned: number;
  numberOfDeploymentSchedules: number;
}
