import { generateGuid } from '../utility';
import { DeploymentSchedulePhaseDto } from './deployment-schedule-phase-dto.model';

export class DeploymentScheduleDetailsDto {
  id?: number;
  name: string;
  phases: DeploymentSchedulePhaseDto[] = [
    {
      ...DeploymentSchedulePhaseDto.default,
      name: 'Phase 1',
      offsetDays: 0,
      guid: generateGuid(),
    },
    {
      ...DeploymentSchedulePhaseDto.default,
      name: 'Phase 2',
      offsetDays: 1,
      guid: generateGuid(),
    },
    {
      ...DeploymentSchedulePhaseDto.default,
      name: 'Phase 3',
      offsetDays: 2,
      guid: generateGuid(),
    },
  ];
  cronTrigger?: string;
}
