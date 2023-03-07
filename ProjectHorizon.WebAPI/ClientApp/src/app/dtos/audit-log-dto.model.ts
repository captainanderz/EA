import { AuditLogCategory } from '../constants/audit-log-category';

export class AuditLogDto {
  category: AuditLogCategory;
  actionText: string;
  sourceIP: string;
  modifiedOn: string;
  user: string;
}
