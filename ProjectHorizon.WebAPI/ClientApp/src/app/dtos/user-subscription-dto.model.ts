import { SubscriptionDto } from './subscription-dto.model';

export class UserSubscriptionDto extends SubscriptionDto {
  readonly userRole: string;
}
