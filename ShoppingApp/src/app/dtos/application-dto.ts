import { RequestState } from './request-state.model';

export class ApplicationDto {
  public id: number;
  public name: string;
  public version: string;
  public iconBase64: string | null;
  public publisher: string;
  public requestState: RequestState;
  public description: string | null;
  public isPrivate: boolean;
}
