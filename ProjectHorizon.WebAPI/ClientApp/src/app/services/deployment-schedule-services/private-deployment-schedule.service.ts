import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { DeploymentScheduleService } from './deployment-schedule.service';

@Injectable({
  providedIn: 'root',
})
export class PrivateDeploymentScheduleService extends DeploymentScheduleService {
  protected type: string = 'private';

  constructor(httpClient: HttpClient) {
    super(httpClient);
  }
}
