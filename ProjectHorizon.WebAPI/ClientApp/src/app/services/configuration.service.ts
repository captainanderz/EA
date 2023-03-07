import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class ConfigurationService {
  private readonly configurationUrl: string = '/api/configuration';

  constructor(private readonly httpClient: HttpClient) {}

  getApplicationInsightsConnectionString(): Observable<string> {
    return this.httpClient.get(
      `${this.configurationUrl}/application-insights-connection-string`,
      { responseType: 'text' }
    );
  }
}
