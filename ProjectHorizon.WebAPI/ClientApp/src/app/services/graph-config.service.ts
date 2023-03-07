import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { GraphConfigDto } from '../dtos/graph-config-dto.model';

@Injectable({
  providedIn: 'root',
})
export class GraphConfigService {
  private readonly graphConfigUrl: string = 'api/graph-config';

  constructor(private readonly httpClient: HttpClient) {}

  createGraphConfig(graphConfigDto: GraphConfigDto): Observable<any> {
    return this.httpClient.post(`${this.graphConfigUrl}`, graphConfigDto);
  }

  removeGraphConfig() {
    return this.httpClient.delete(`${this.graphConfigUrl}`);
  }

  hasGraphConfig(): Observable<boolean> {
    return this.httpClient.get<boolean>(`${this.graphConfigUrl}`);
  }

  checkGraphConfigStatus(): Observable<any> {
    return this.httpClient.get(`${this.graphConfigUrl}/status`);
  }
}
