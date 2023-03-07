import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedResult } from 'src/app/dtos/paged-result.model';
import { RequestState } from 'src/app/dtos/request-state.model';
import { ShopRequestCountDto } from 'src/app/dtos/shop-request-count-dto.model';
import { ShopRequestDto } from 'src/app/dtos/shop-request-dto.model';

@Injectable({
  providedIn: 'root',
})
export class ShoppingRequestsService {
  private readonly shoppingUrl: string = `/api/shopping`;
  private readonly requestsUrl: string = `${this.shoppingUrl}/requests`;

  constructor(private readonly httpClient: HttpClient) {}

  getRequestsPaged(
    pageNumber: number,
    pageSize: number,
    searchTerm: string,
    stateFilter: RequestState
  ): Observable<PagedResult<ShopRequestDto>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString())
      .set('searchTerm', searchTerm.trim())
      .set('requestStateFilter', stateFilter.toString());

    return this.httpClient.get<PagedResult<ShopRequestDto>>(this.requestsUrl, {
      params,
    });
  }

  approveRequest(request: ShopRequestDto) {
    const type = request.isPrivate ? 'private' : 'public';

    return this.httpClient.put(`${this.requestsUrl}/${type}/${request.id}`, {});
  }

  rejectRequest(request: ShopRequestDto) {
    const type = request.isPrivate ? 'private' : 'public';

    return this.httpClient.delete(`${this.requestsUrl}/${type}/${request.id}`);
  }

  getShopRequestCount(): Observable<ShopRequestCountDto> {
    return this.httpClient.get<ShopRequestCountDto>(
      `${this.requestsUrl}/count`
    );
  }
}
