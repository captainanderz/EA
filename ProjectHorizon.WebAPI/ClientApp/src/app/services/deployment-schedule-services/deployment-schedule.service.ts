import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { DeploymentScheduleDetailsDto } from 'src/app/dtos/deployment-schedule-details-dto.model';
import { DeploymentScheduleDto } from 'src/app/dtos/deployment-schedule-dto.model';
import { DeploymentScheduleRemoveDto } from 'src/app/dtos/deployment-schedule-remove-dto.model';
import { DeploymentScheduleClearDto } from 'src/app/dtos/deployment-schedule-clear-dto.model';
import { PagedResult } from 'src/app/dtos/paged-result.model';
import { DeploymentScheduleAssignDto } from 'src/app/dtos/deployment-schedule-assign-dto.model';

@Injectable({
  providedIn: 'root',
})
export abstract class DeploymentScheduleService {
  protected abstract readonly type: string;
  private readonly deploymentSchedulesUrl: string = '/api/deployment-schedules';

  constructor(private readonly httpClient: HttpClient) {}

  getDeploymentSchedulesPaged(
    pageNumber: number,
    pageSize: number,
    searchTerm: string
  ): Observable<PagedResult<DeploymentScheduleDto>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString())
      .set('searchTerm', searchTerm.trim());

    return this.httpClient.get<PagedResult<DeploymentScheduleDto>>(
      this.deploymentSchedulesUrl,
      {
        params,
      }
    );
  }

  getAllDeploymentScheduleIds(): Observable<Array<number>> {
    return this.httpClient.get<Array<number>>(
      `${this.deploymentSchedulesUrl}/ids`
    );
  }

  getDeploymentScheduleById(
    deploymentScheduleId: number
  ): Observable<DeploymentScheduleDetailsDto> {
    return this.httpClient.get<DeploymentScheduleDetailsDto>(
      `${this.deploymentSchedulesUrl}/${deploymentScheduleId}`
    );
  }

  filterDeploymentSchedulesByName(
    deploymentScheduleName: string
  ): Observable<ReadonlyArray<DeploymentScheduleDto>> {
    const params = new HttpParams().set(
      'deploymentScheduleName',
      deploymentScheduleName
    );

    return this.httpClient.get<ReadonlyArray<DeploymentScheduleDto>>(
      `${this.deploymentSchedulesUrl}/filter`,
      {
        params,
      }
    );
  }

  editDeploymentSchedule(
    deploymentId: number,
    profile: DeploymentScheduleDetailsDto
  ): Observable<number> {
    return this.httpClient.post<number>(
      `${this.deploymentSchedulesUrl}/${deploymentId}/edit`,
      profile
    );
  }

  addDeploymentSchedule(
    schedule: DeploymentScheduleDetailsDto
  ): Observable<number> {
    return this.httpClient.post<number>(
      `${this.deploymentSchedulesUrl}/add`,
      schedule
    );
  }

  copyDeploymentSchedule(
    deploymentSchedulesIds: Array<number>
  ): Observable<number> {
    return this.httpClient.post<number>(
      `${this.deploymentSchedulesUrl}/copy`,
      deploymentSchedulesIds
    );
  }

  deleteDeploymentSchedule(dto: DeploymentScheduleRemoveDto): Observable<any> {
    return this.httpClient.request('delete', `${this.deploymentSchedulesUrl}`, {
      body: dto,
    });
  }

  assignDeploymentScheduleToApplications(
    deploymentScheduleId: number,
    dto: DeploymentScheduleAssignDto
  ): Observable<void> {
    return this.httpClient.post<void>(
      `${this.deploymentSchedulesUrl}/${deploymentScheduleId}/assign-${this.type}-applications`,
      dto
    );
  }

  clearDeploymentScheduleFromApplications(
    dto: DeploymentScheduleClearDto
  ): Observable<void> {
    return this.httpClient.post<void>(
      `${this.deploymentSchedulesUrl}/clear-${this.type}-applications`,
      dto
    );
  }

  deleteDeploymentSchedulePatchAppFromApplications(
    ids: Array<number>
  ): Observable<void> {
    return this.httpClient.post<void>(
      `${this.deploymentSchedulesUrl}/delete-${this.type}-application-patch-apps`,
      ids
    );
  }
}
