import { Version } from '@angular/compiler';
import { DeploymentStatus } from '../constants/deployment-status';
import { PhaseState } from '../constants/phase-state';
import { BaseEntityIdNumber } from './base-entity-id-number.model';
import { DeploymentScheduleDto } from './deployment-schedule-dto.model';

export class ApplicationDto extends BaseEntityIdNumber {
  name: string;
  version: string;
  publisher: string;
  runAs32Bit: boolean;
  informationUrl: string;
  notes: string;
  iconBase64: string;
  language: string;
  deploymentStatus: DeploymentStatus | undefined;
  architecture: string;
  assignedProfileName: string;
  assignedDeploymentSchedule?: DeploymentScheduleDto;
  isInShop: boolean;
  description: string;
  existingVersion: string | undefined;
  assignedDeploymentScheduleInProgress: boolean;
  assignedDeploymentSchedulePhaseState: PhaseState;
  assignedDeploymentSchedulePhaseName?: string;
}

export const displayArchitecture = (architecture: string): string =>
  architecture === 'x64' ? '64-bit' : '32-bit';
