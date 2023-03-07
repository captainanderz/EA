import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { SubscriptionDto } from '../dtos/subscription-dto.model';
import { Response } from '../dtos/response.model';
import { FarPayResult } from '../dtos/far-pay-result.model';
import { PagedResult } from '../dtos/paged-result.model';
import { BillingInfoDto } from '../dtos/billing-info-dto.model';
import { SubscriptionDetailsDto } from '../dtos/subscription-details-dto.model';
import { OrganizationDto } from '../dtos/organization-dto.model';
import { SubscriptionConsentDto } from '../dtos/subscription-consent-dto.model';
import { ShopGroupPrefixDto } from '../dtos/shop-group-prefix-dto';

@Injectable({
  providedIn: 'root',
})
export class SubscriptionService {
  private readonly subscriptionsUrl: string = 'api/subscriptions';

  constructor(private readonly httpClient: HttpClient) {}

  getSubscriptionsPaged(
    pageNumber: number,
    pageSize: number,
    searchTerm: string
  ): Observable<PagedResult<SubscriptionDto>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString())
      .set('searchTerm', searchTerm.trim());
    return this.httpClient.get<PagedResult<SubscriptionDto>>(
      `${this.subscriptionsUrl}/paged`,
      { params }
    );
  }

  filterSubscriptionsByName(
    subscriptionName: string
  ): Observable<ReadonlyArray<SubscriptionDto>> {
    if (!subscriptionName.trim()) return of([]);

    const params = new HttpParams().set('subscriptionName', subscriptionName);

    return this.httpClient.get<ReadonlyArray<SubscriptionDto>>(
      `${this.subscriptionsUrl}/filter`,
      {
        params,
      }
    );
  }

  updateSubscriptionAutoUpdate(autoUpdate: boolean): Observable<boolean> {
    return this.httpClient.patch<boolean>(
      `${this.subscriptionsUrl}/auto-update/${autoUpdate}`,
      null
    );
  }

  updateSubscriptionManualApprove(manualApprove: boolean): Observable<boolean> {
    return this.httpClient.patch<boolean>(
      `${this.subscriptionsUrl}/manual-approve/${manualApprove}`,
      null
    );
  }

  updateLastDigits(subscriptionId: string): Observable<Response<FarPayResult>> {
    return this.httpClient.patch<Response<FarPayResult>>(
      `${this.subscriptionsUrl}/update-last-digits/${subscriptionId}`,
      null
    );
  }

  getFarpayOrder(subscriptionId: string): Observable<Response<FarPayResult>> {
    const params = new HttpParams().set('subscriptionId', subscriptionId);

    return this.httpClient.get<Response<FarPayResult>>(
      `${this.subscriptionsUrl}/farpay-order`,
      { params }
    );
  }

  updateBillingInfo(billingInfoDto: BillingInfoDto): Observable<any> {
    return this.httpClient.patch(
      `${this.subscriptionsUrl}/billing-information`,
      billingInfoDto
    );
  }

  getSubscriptionDetails(): Observable<SubscriptionDetailsDto> {
    return this.httpClient.get<SubscriptionDetailsDto>(
      `${this.subscriptionsUrl}/details`
    );
  }

  createNewFarpayOrder(): Observable<FarPayResult> {
    return this.httpClient.post<FarPayResult>(
      `${this.subscriptionsUrl}/new-farpay-order`,
      null
    );
  }

  cancelSubscription(): Observable<any> {
    return this.httpClient.post(
      `${this.subscriptionsUrl}/cancel-subscription`,
      null
    );
  }

  reactivateSubscription(): Observable<any> {
    return this.httpClient.post(
      `${this.subscriptionsUrl}/reactivate-subscription`,
      null
    );
  }

  getLogo(): Observable<any> {
    return this.httpClient.get(`${this.subscriptionsUrl}/logo`, {
      responseType: 'blob',
    });
  }

  changeLogo(picture: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', picture, picture.name);
    return this.httpClient.put(`${this.subscriptionsUrl}/logo`, formData);
  }

  deleteLogo(): Observable<any> {
    return this.httpClient.delete(`${this.subscriptionsUrl}/logo`);
  }

  getOrganization(): Observable<OrganizationDto> {
    return this.httpClient.get<OrganizationDto>(
      `${this.subscriptionsUrl}/organization`
    );
  }

  addConsent(dto: SubscriptionConsentDto): Observable<any> {
    return this.httpClient.post(`${this.subscriptionsUrl}/consents`, dto);
  }

  getConsents(): Observable<ReadonlyArray<SubscriptionConsentDto>> {
    return this.httpClient.get<ReadonlyArray<SubscriptionConsentDto>>(
      `${this.subscriptionsUrl}/consents`
    );
  }

  getShopGroupPrefix(): Observable<ShopGroupPrefixDto> {
    return this.httpClient.get<ShopGroupPrefixDto>(
      `${this.subscriptionsUrl}/shopGroupPrefix`
    );
  }

  updateShopGroupPrefix(prefix: string): Observable<any> {
    return this.httpClient.put(`${this.subscriptionsUrl}/shopGroupPrefix`, {
      prefix,
    });
  }
}
