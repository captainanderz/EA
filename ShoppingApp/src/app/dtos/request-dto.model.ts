import { BaseEntityIdNumber } from './base-entity-id-number.model';
import { RequestState } from './request-state.model';

export class RequestDto extends BaseEntityIdNumber {
  applicationName: string;
  requesterName: string;
  stateId: RequestState;
  applicationId: number;
  subscriptionId: string;
  isPrivate: boolean;
}
