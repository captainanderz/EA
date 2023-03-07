import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { mergeMap, Observable, of } from 'rxjs';
import { ApiRoutes } from '../constants/api-routes';
import { UserDto } from '../dtos/user-dto.model';
import { UserStore } from './user.store';
import { Response } from '../dtos/response.model';
import { UserStoreKeys } from '../constants/user-store-keys';
import { MsalService } from '@azure/msal-angular';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  // Constructor
  public constructor(
    private readonly httpClient: HttpClient,
    private readonly userStore: UserStore,
    private msalService: MsalService
  ) {}

  // Methods
  // Handles the logic of logging in a user
  public login(): Observable<UserDto | undefined> {
    return this.httpClient.get<Response<UserDto>>(ApiRoutes.login).pipe(
      mergeMap((response) => {
        if (!response.isSuccessful) {
          return of(undefined);
        }

        this.userStore.setLoggedInUser(response.dto);
        localStorage.setItem(
          UserStoreKeys.accessToken,
          response.dto.accessToken
        );

        return of(response.dto);
      })
    );
  }

  // Handles the logic of login out a user
  public logout(): void {
    localStorage.removeItem(UserStoreKeys.accessToken);
    this.userStore.logout();
    this.msalService.logoutRedirect();
  }

  // Sets the Azure access token of the user
  public setAzureAccessToken(accessToken: string): void {
    localStorage.setItem(UserStoreKeys.azureAccessToken, accessToken);
  }

  // Sets the access token of the user
  public setAccessToken(accessToken: string): void {
    localStorage.setItem(UserStoreKeys.accessToken, accessToken);
  }

  // Checks if the user has an Azure access token
  public hasAzureAccessToken(): boolean {
    return localStorage.getItem(UserStoreKeys.azureAccessToken) != null;
  }

  // Checks if the user has an access token
  public hasAccessToken(): boolean {
    return localStorage.getItem(UserStoreKeys.accessToken) != null;
  }

  // Checks if the user is logged in
  public isLoggedIn(): Observable<boolean> {
    return this.userStore
      .getLoggedInUser()
      .pipe(mergeMap((user) => of(user != undefined && this.hasAccessToken())));
  }
}
