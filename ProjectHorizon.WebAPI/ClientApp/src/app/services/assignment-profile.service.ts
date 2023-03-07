import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AssignmentProfileDetailsDto } from '../dtos/assignment-profile-details-dto.model';
import { AssignmentProfileDto } from '../dtos/assignment-profile-dto.model';
import { NewAssignmentProfileDto } from '../dtos/new-assignment-profile-dto.model';
import { PagedResult } from '../dtos/paged-result.model';

@Injectable({
  providedIn: 'root',
})
export class AssignmentProfileService {
  private readonly assignmentProfilesUrl: string = '/api/assignment-profiles';

  constructor(private readonly httpClient: HttpClient) {}

  getAssignmentsPaged(
    pageNumber: number,
    pageSize: number,
    searchTerm: string
  ): Observable<PagedResult<AssignmentProfileDto>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString())
      .set('searchTerm', searchTerm.trim());

    return this.httpClient.get<PagedResult<AssignmentProfileDto>>(
      this.assignmentProfilesUrl,
      {
        params,
      }
    );
  }

  assignAssignmentProfileToPrivateApplications(
    assignmentProfileId: number,
    applicationIds: Array<number>
  ): Observable<void> {
    return this.httpClient.post<void>(
      `${this.assignmentProfilesUrl}/${assignmentProfileId}/assign-private-applications`,
      applicationIds
    );
  }

  assignAssignmentProfileToPublicApplications(
    assignmentProfileId: number,
    applicationIds: Array<number>
  ): Observable<void> {
    return this.httpClient.post<void>(
      `${this.assignmentProfilesUrl}/${assignmentProfileId}/assign-public-applications`,
      applicationIds
    );
  }

  getAllAssignmentsIds(): Observable<Array<number>> {
    return this.httpClient.get<Array<number>>(
      `${this.assignmentProfilesUrl}/ids`
    );
  }

  getAssignmentsCount(): Observable<any> {
    return this.httpClient.get<number>(`${this.assignmentProfilesUrl}/count`);
  }

  editAssignmentProfile(
    assignmentId: number,
    profile: NewAssignmentProfileDto
  ): Observable<number> {
    return this.httpClient.post<number>(
      `${this.assignmentProfilesUrl}/${assignmentId}/edit`,
      profile
    );
  }

  copyAssignmentProfiles(
    assignmentProfileIds: Array<number>
  ): Observable<number> {
    return this.httpClient.post<number>(
      `${this.assignmentProfilesUrl}/copy`,
      assignmentProfileIds
    );
  }

  deleteAssignmentProfiles(
    assignmentProfileIds: Array<number>
  ): Observable<any> {
    return this.httpClient.request('delete', `${this.assignmentProfilesUrl}`, {
      body: assignmentProfileIds,
    });
  }

  clearAssignmentProfileFromPrivateApplications(
    applicationIds: Array<number>
  ): Observable<void> {
    return this.httpClient.post<void>(
      `${this.assignmentProfilesUrl}/clear-private-applications`,
      applicationIds
    );
  }

  clearAssignmentProfileFromPublicApplications(
    applicationIds: Array<number>
  ): Observable<void> {
    return this.httpClient.post<void>(
      `${this.assignmentProfilesUrl}/clear-public-applications`,
      applicationIds
    );
  }

  addAssignmentProfile(profile: NewAssignmentProfileDto): Observable<number> {
    return this.httpClient.post<number>(
      `${this.assignmentProfilesUrl}/add`,
      profile
    );
  }

  getAssignmentProfileById(
    assignmentId: number
  ): Observable<AssignmentProfileDetailsDto> {
    return this.httpClient.get<AssignmentProfileDetailsDto>(
      `${this.assignmentProfilesUrl}/${assignmentId}`
    );
  }

  getAllAssignmentProfilesIds(): Observable<Array<number>> {
    return this.httpClient.get<Array<number>>(
      `${this.assignmentProfilesUrl}/ids`
    );
  }

  filterAssignmentsByName(
    assignmentProfileName: string
  ): Observable<ReadonlyArray<AssignmentProfileDto>> {
    const params = new HttpParams().set(
      'assignmentProfileName',
      assignmentProfileName
    );

    return this.httpClient.get<ReadonlyArray<AssignmentProfileDto>>(
      `${this.assignmentProfilesUrl}/filter`,
      {
        params,
      }
    );
  }
}
