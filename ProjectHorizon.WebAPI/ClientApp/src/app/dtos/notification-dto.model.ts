import { NotificationType } from '../constants/notification-type';
import { BaseEntityIdNumber } from './base-entity-id-number.model';

export class NotificationDto extends BaseEntityIdNumber {
  type: NotificationType;
  message: string;
  isRead: boolean;
  forPrivateRepository: boolean;
}
