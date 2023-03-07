import { HttpClient, HttpEvent, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApprovalDto } from '../dtos/approval-dto.model';
import { PagedResult } from '../dtos/paged-result.model';

@Injectable({
  providedIn: 'root',
})
export class ApprovalService {
  private readonly approvalsUrl: string = '/api/approvals';

  constructor(private readonly httpClient: HttpClient) {}

  getApprovalsPaged(
    pageNumber: number,
    pageSize: number
  ): Observable<PagedResult<ApprovalDto>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    return this.httpClient.get<PagedResult<ApprovalDto>>(this.approvalsUrl, {
      params,
    });
  }

  getAllApprovalIds(): Observable<Array<number>> {
    return this.httpClient.get<Array<number>>(`${this.approvalsUrl}/ids`);
  }

  getApprovalCount(): Observable<any> {
    return this.httpClient.get<number>(`${this.approvalsUrl}/count`);
  }

  approveItems(approvalIds: ReadonlyArray<number>): Observable<any> {
    return this.httpClient.put(`${this.approvalsUrl}/approve`, approvalIds);
  }

  rejectItems(approvalIds: ReadonlyArray<number>): Observable<any> {
    return this.httpClient.put(`${this.approvalsUrl}/reject`, approvalIds);
  }
}
