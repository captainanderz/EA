import { BaseEntityIdNumber } from './base-entity-id-number.model';

export class ApprovalDto extends BaseEntityIdNumber {
  name: string;
  deployedVersion: string;
  newVersion: string;
  iconBase64: string;
  architecture: string;
}
