import { BillingInfoDto } from './billing-info-dto.model';

export class SubscriptionDetailsDto extends BillingInfoDto {
  subscriptionId: string;
  state: string;
  dueAmount: number;
  creditCardDigits: string;
  deviceCount: number;
  logoSmall: string;
  daysUntilPayment: number;
  dueDate: Date;
  shopGroupPrefix: string;
}
