import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PublicApplicationDto } from 'src/app/dtos/public-application-dto.model';
import { ApplicationService } from './application.service';

@Injectable({
  providedIn: 'root',
})
export class PublicApplicationService extends ApplicationService<PublicApplicationDto> {
  protected readonly applicationsUrl = 'api/public-applications';

  constructor(httpClient: HttpClient) {
    super(httpClient);
  }

  updateSubscriptionPublicApplicationAutoUpdate(
    publicApplicationId: number,
    autoUpdate: boolean
  ): Observable<boolean> {
    return this.httpClient.patch<boolean>(
      `${this.applicationsUrl}/${publicApplicationId}/auto-update/${autoUpdate}`,
      null
    );
  }

  updateSubscriptionPublicApplicationManualApprove(
    publicApplicationId: number,
    manualApprove: boolean
  ): Observable<boolean> {
    return this.httpClient.patch<boolean>(
      `${this.applicationsUrl}/${publicApplicationId}/manual-approve/${manualApprove}`,
      null
    );
  }
}
