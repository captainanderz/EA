import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { UserStoreKeys } from '../constants/user-store-keys';
import { RegistrationDto } from '../dtos/registration-dto.model';

@Injectable({
  providedIn: 'root',
})
export class AzureAuthService {
  private readonly azureAuthUrl: string = 'api/azure-auth';
  private authHeader: HttpHeaders;

  constructor(private readonly httpClient: HttpClient) {
    if (localStorage.getItem(UserStoreKeys.azureAccessToken) !== null) {
      this.authHeader = new HttpHeaders().set(
        'Authorization',
        'Bearer ' + localStorage.getItem(UserStoreKeys.azureAccessToken)
      );
    }
  }

  register(azureRegistrationDto: RegistrationDto): Observable<any> {
    return this.httpClient.post(
      `${this.azureAuthUrl}/register`,
      azureRegistrationDto,
      {
        headers: this.authHeader,
      }
    );
  }

  login(): Observable<any> {
    return this.httpClient.get(`${this.azureAuthUrl}/login`, {
      headers: this.authHeader,
    });
  }

  setAzureAccessToken(accessToken: string) {
    localStorage.setItem(UserStoreKeys.azureAccessToken, accessToken);
    this.authHeader = new HttpHeaders().set(
      'Authorization',
      'Bearer ' + accessToken
    );
  }
}
