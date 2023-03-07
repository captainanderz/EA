import { HttpClient, HttpEvent, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApplicationDto } from 'src/app/dtos/application-dto.model';
import { PagedResult } from 'src/app/dtos/paged-result.model';

export abstract class ApplicationService<TApplication extends ApplicationDto> {
  protected abstract readonly applicationsUrl: string;

  constructor(protected readonly httpClient: HttpClient) {}

  getApplicationsPaged(
    pageNumber: number,
    pageSize: number,
    searchTerm: string
  ): Observable<PagedResult<TApplication>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString())
      .set('searchTerm', searchTerm.trim());

    return this.httpClient.get<PagedResult<TApplication>>(
      this.applicationsUrl,
      {
        params,
      }
    );
  }

  uploadApplication(application: File): Observable<HttpEvent<TApplication>> {
    const formData = new FormData();
    formData.append('file', application, application.name);

    return this.httpClient.post<TApplication>(
      `${this.applicationsUrl}/upload`,
      formData,
      {
        observe: 'events',
        reportProgress: true,
      }
    );
  }

  deployApplications(applicationIds: ReadonlyArray<number>): Observable<any> {
    return this.httpClient.post(
      `${this.applicationsUrl}/deploy`,
      applicationIds
    );
  }

  addApplication(application: TApplication): Observable<number> {
    return this.httpClient.put<number>(
      `${this.applicationsUrl}/add-or-update`,
      application
    );
  }

  deleteApplications(applicationIds: Array<number>): Observable<any> {
    return this.httpClient.request('delete', `${this.applicationsUrl}`, {
      body: applicationIds,
    });
  }

  getDownloadUriForApplication(applicationId: number): Observable<string> {
    return this.httpClient.get(
      `${this.applicationsUrl}/${applicationId}/download-uri`,
      {
        responseType: 'text',
      }
    );
  }

  getAllApplicationIds(): Observable<Array<number>> {
    return this.httpClient.get<Array<number>>(`${this.applicationsUrl}/ids`);
  }
}
