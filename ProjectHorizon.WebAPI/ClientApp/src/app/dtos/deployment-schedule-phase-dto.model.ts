import { generateGuid } from '../utility';
import { BaseEntityId } from './base-entity-id.model';
import { BaseEntity } from './base-entity.model';

export class DeploymentSchedulePhaseDto extends BaseEntityId<
  number | undefined
> {
  static default: DeploymentSchedulePhaseDto = {
    id: undefined,
    name: 'Default',
    assignmentProfileId: undefined,
    assignmentProfileName: undefined,
    offsetDays: 0,
    useRequirementScript: true,
    guid: generateGuid(),
    ...BaseEntity.default,
  };

  name: string;
  assignmentProfileId?: number;
  assignmentProfileName?: string;
  offsetDays: number;
  useRequirementScript: boolean;
  guid: string;
}
