import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { DeploymentScheduleService } from './deployment-schedule.service';

@Injectable({
  providedIn: 'root',
})
export class PublicDeploymentScheduleService extends DeploymentScheduleService {
  protected type: string = 'public';

  constructor(httpClient: HttpClient) {
    super(httpClient);
  }
}
