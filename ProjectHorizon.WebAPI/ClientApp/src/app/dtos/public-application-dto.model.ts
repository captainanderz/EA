import { ApplicationDto } from './application-dto.model';

export class PublicApplicationDto extends ApplicationDto {
  autoUpdate: boolean;
  manualApprove: boolean;
}
