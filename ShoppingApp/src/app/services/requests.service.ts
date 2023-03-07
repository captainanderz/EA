import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiRoutes } from '../constants/api-routes';
import { PagedResult } from '../dtos/paged-result.model';
import { RequestState } from '../dtos/request-state.model';
import { RequestDto } from '../dtos/request-dto.model';

@Injectable({
  providedIn: 'root',
})
export class RequestsService {
  // Constructor
  constructor(private readonly httpClient: HttpClient) {}

  // Methods
  // Gets all the application paged and apply the state filter
  getPaged(
    pageNumber: number,
    pageSize: number,
    searchTerm: string,
    stateFilter: RequestState
  ): Observable<PagedResult<RequestDto>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString())
      .set('searchTerm', searchTerm.trim())
      .set('requestStateFilter', stateFilter.toString());

    return this.httpClient.get<PagedResult<RequestDto>>(ApiRoutes.requests, {
      params,
    });
  }

  // Handles the approve logic of a request
  approve(request: RequestDto) {
    const type = request.isPrivate ? 'private' : 'public';

    return this.httpClient.put(
      `${ApiRoutes.requests}/${type}/${request.id}`,
      {}
    );
  }

  // Handles the reject logic of the request
  reject(request: RequestDto) {
    const type = request.isPrivate ? 'private' : 'public';

    return this.httpClient.delete(
      `${ApiRoutes.requests}/${type}/${request.id}`
    );
  }
}
