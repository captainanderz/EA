import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class TermsAndConditionsService {
  private readonly localUrl: string = '/assets/legal/terms';
  private readonly url: string = '/api/terms';

  constructor(private readonly httpClient: HttpClient) {}

  get(version: string): Observable<string> {
    return this.httpClient.get(
      `${this.localUrl}/terms-and-conditions-${version}.html`,
      {
        responseType: 'text',
      }
    );
  }

  accept(): Observable<any> {
    return this.httpClient.post(`${this.url}/accept`, {});
  }

  checkAccepted(): Observable<boolean> {
    return this.httpClient.get<boolean>(`${this.url}/check-accepted`);
  }
}
