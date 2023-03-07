import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { NgbDate, NgbDateParserFormatter } from '@ng-bootstrap/ng-bootstrap';
import { Observable } from 'rxjs';
import { AuditLogCategory } from '../constants/audit-log-category';
import { AuditLogDto } from '../dtos/audit-log-dto.model';
import { PagedResult } from '../dtos/paged-result.model';

@Injectable({
  providedIn: 'root',
})
export class AuditLogService {
  private readonly url: string = 'api/audit-logs';

  constructor(
    private readonly httpClient: HttpClient,
    private readonly dateFormatter: NgbDateParserFormatter
  ) {}

  getPaged(
    pageNumber: number,
    pageSize: number,
    searchTerm: string,
    fromDate: NgbDate | null,
    toDate: NgbDate | null,
    category: AuditLogCategory
  ): Observable<PagedResult<AuditLogDto>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString())
      .set('searchTerm', searchTerm.trim())
      .set('fromDate', this.dateFormatter.format(fromDate))
      .set('toDate', this.dateFormatter.format(toDate))
      .set('category', category);

    return this.httpClient.get<PagedResult<AuditLogDto>>(`${this.url}/paged`, {
      params,
    });
  }

  getCsv(
    fromDate: NgbDate | null,
    toDate: NgbDate | null,
    searchTerm: string,
    category: string
  ): Observable<Blob> {
    const params = new HttpParams()
      .set('fromDate', this.dateFormatter.format(fromDate))
      .set('toDate', this.dateFormatter.format(toDate))
      .set('searchTerm', searchTerm.trim())
      .set('category', category);

    return this.httpClient.get(`${this.url}/csv`, {
      params,
      responseType: 'blob',
    });
  }

  adminConsentAudit(): Observable<any> {
    return this.httpClient.post(`${this.url}/admin-consent`, null);
  }
}
