import { BaseEntity } from './base-entity.model';
import { SubscriptionDto } from './subscription-dto.model';
import { UserSubscriptionDto } from './user-subscription-dto.model';

export class UserDto extends BaseEntity {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber: string | null;
  profilePictureSmall: string | null;
  subscriptionId: string;
  userRole: string;
  accessToken: string;
  subscriptions: ReadonlyArray<UserSubscriptionDto> = [];
  currentSubscriptionIndex: number;
  twoFactorRequired: boolean;

  static currentSubscription(dto: UserDto): UserSubscriptionDto {
    return dto.subscriptions[dto.currentSubscriptionIndex];
  }
}

export type ReadonlyUserDto = Readonly<UserDto>;
