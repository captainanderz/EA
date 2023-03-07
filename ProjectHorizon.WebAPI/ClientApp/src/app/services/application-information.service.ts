import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApplicationInformationDto } from '../dtos/application-information-dto';

@Injectable({
  providedIn: 'root',
})
export class ApplicationInformationService {
  private readonly url: string = '/api/application-information';

  constructor(private readonly httpClient: HttpClient) {}

  get(): Observable<ApplicationInformationDto> {
    return this.httpClient.get<ApplicationInformationDto>(`${this.url}`);
  }
}
