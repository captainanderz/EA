import { BaseEntityIdNumber } from './base-entity-id-number.model';

export class DeploymentScheduleDto extends BaseEntityIdNumber {
  name: string;
  currentPhaseName?: string;
  numberOfApplicationsAssigned: number;
  isDeleted: boolean;
  isInProgress: boolean;
  cronTrigger?: string;
}
