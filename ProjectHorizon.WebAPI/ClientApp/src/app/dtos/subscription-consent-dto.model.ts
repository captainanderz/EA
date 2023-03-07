import { BaseEntity } from './base-entity.model';

export class SubscriptionConsentDto extends BaseEntity {
  id?: number;
  tenantId: string;
}
