export class PasswordResetDto {
  email: string;
  token: string;
  newPassword: string;
  repeatPassword: string;
}
