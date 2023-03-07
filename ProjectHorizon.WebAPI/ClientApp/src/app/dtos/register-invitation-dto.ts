export class RegisterInvitationDto {
  password: string;
  repeatPassword: string;
  email: string;
  emailToken: string;
  subscriptionName: string;
  acceptedTerms: boolean;
}
