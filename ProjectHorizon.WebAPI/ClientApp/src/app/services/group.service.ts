import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { CursorPagedResult } from '../dtos/cursor-paged-result';
import { GroupDto } from '../dtos/group-dto.model';

@Injectable({
  providedIn: 'root',
})
export class GroupService {
  private readonly groupsUrl: string = 'api/azure-groups';

  constructor(private readonly httpClient: HttpClient) {}

  filterGroupsByName(
    groupName: string,
    nextPageLink: string | undefined
  ): Observable<CursorPagedResult<GroupDto>> {
    const params = new HttpParams()
      .set('groupName', groupName)
      .set('nextPageLink', nextPageLink || '');

    return this.httpClient.get<CursorPagedResult<GroupDto>>(
      `${this.groupsUrl}/filter`,
      {
        params,
      }
    );
  }
}
