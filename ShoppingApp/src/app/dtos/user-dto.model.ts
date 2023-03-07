export class UserDto {
  id: string;
  subscriptionId: string;
  accessToken: string;
  refreshToken: string;
}

export type ReadonlyUserDto = Readonly<UserDto>;
