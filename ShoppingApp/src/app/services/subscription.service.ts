import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiRoutes } from '../constants/api-routes';
import { SubscriptionDto } from '../dtos/subscription-dto.model';

@Injectable({
  providedIn: 'root',
})
export class SubscriptionService {
  // Constructor
  constructor(private readonly httpClient: HttpClient) {}

  // Methods
  // Get the current subscription
  get(): Observable<SubscriptionDto> {
    return this.httpClient.get<SubscriptionDto>(
      `${ApiRoutes.subscriptionLogo}`
    );
  }
}
