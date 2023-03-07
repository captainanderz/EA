import { ApplicationDto } from './application-dto';

export class ApplicationDetailsDto extends ApplicationDto {
  public developer: string;
  public informationUrl: string;
  public notes: string;
  public architecture: string;
  public createdOn: Date;
  public modifiedOn: Date;
}
