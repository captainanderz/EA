import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { PrivateApplicationDto } from 'src/app/dtos/private-application-dto.model';
import { ApplicationService } from './application.service';

@Injectable({
  providedIn: 'root',
})
export class PrivateApplicationService extends ApplicationService<PrivateApplicationDto> {
  protected readonly applicationsUrl = 'api/private-applications';

  constructor(httpClient: HttpClient) {
    super(httpClient);
  }
}
