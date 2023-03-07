import { BaseEntity } from './base-entity.model';

export class SubscriptionDto extends BaseEntity {
  readonly id: string;
  readonly name: string;
  readonly state: string;
  readonly numberOfUsers: number;
  readonly deviceCount?: number;
  logoSmall: string;
  globalAutoUpdate: boolean;
  globalManualApprove: boolean;
  azureIntegrationDone: boolean;
  shopGroupPrefix: string;
}
