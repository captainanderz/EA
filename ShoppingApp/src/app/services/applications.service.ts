import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiRoutes } from '../constants/api-routes';
import { HttpClient, HttpParams } from '@angular/common/http';
import { ApplicationDto } from '../dtos/application-dto';
import { PagedResult } from '../dtos/paged-result.model';
import { ApplicationDetailsDto } from '../dtos/application-details-dto.model';
import { RequestState } from '../dtos/request-state.model';

@Injectable({
  providedIn: 'root',
})
export class ApplicationsService {
  // Constructor
  constructor(private readonly httpClient: HttpClient) {}

  // Methods

  // Pagination logic
  public getPaged(
    pageNumber: number,
    pageSize: number,
    searchTerm: string,
    stateFilter: RequestState
  ): Observable<PagedResult<ApplicationDto>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString())
      .set('searchTerm', searchTerm.trim())
      .set('requestStateFilter', stateFilter);

    return this.httpClient.get<PagedResult<ApplicationDto>>(
      `${ApiRoutes.applications}`,
      { params }
    );
  }

  // Handles the request logic
  public request(application: ApplicationDto) {
    const type = application.isPrivate ? 'private' : 'public';

    return this.httpClient.post(
      `${ApiRoutes.applications}/${type}/${application.id}`,
      {}
    );
  }

  // Handles the logic of displaying of the details of an application
  public getDetails(
    id: number,
    isPrivate: boolean
  ): Observable<ApplicationDetailsDto> {
    const type = isPrivate ? 'private' : 'public';

    return this.httpClient.get<ApplicationDetailsDto>(
      `${ApiRoutes.applications}/${type}/${id}`
    );
  }
}
