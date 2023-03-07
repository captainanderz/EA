import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { map, mergeMap, delay } from 'rxjs/operators';
import { AppRoutes } from '../constants/app-routes';
import { UserStoreKeys } from '../constants/user-store-keys';
import { UserDto } from '../dtos/user-dto.model';
import { Response } from '../dtos/response.model';
import { AuthService } from './auth.service';
import { UserService } from './user.service';

@Injectable({
  providedIn: 'root',
})
export class UserStore {
  private readonly loggedInUser = new BehaviorSubject<UserDto | undefined>(
    undefined
  );

  private refreshTokenTimeout: any;

  constructor(
    private readonly userService: UserService,
    private readonly router: Router,
    private readonly authService: AuthService
  ) {}

  getLoggedInUser(): Observable<UserDto | undefined> {
    var userFromStorage = localStorage.getItem(UserStoreKeys.loggedInUser);

    if (userFromStorage) {
      let userDto = JSON.parse(userFromStorage) as UserDto;

      this.loggedInUser.next(userDto);

      if (!this.refreshTokenTimeout) {
        this.startRefreshTokenTimer(userDto.accessToken);
      }
    }

    return this.loggedInUser.asObservable();
  }

  setLoggedInUser(
    newUserDto: UserDto,
    overrideSubscriptionIndex: boolean = false
  ) {
    this.saveUserInLocalStorage(newUserDto, overrideSubscriptionIndex);

    this.loggedInUser.next(newUserDto);
  }

  saveUserInLocalStorage(
    newUserDto: UserDto,
    overrideSubscriptionIndex: boolean = false
  ) {
    const currentUserString = localStorage.getItem(UserStoreKeys.loggedInUser);

    if (overrideSubscriptionIndex || !currentUserString) {
      newUserDto.currentSubscriptionIndex = newUserDto.subscriptions.findIndex(
        (sub) => sub.id === newUserDto.subscriptionId
      );
    } else {
      const currentUser = JSON.parse(currentUserString) as UserDto;

      let newIndex = currentUser.currentSubscriptionIndex;
      const length = newUserDto.subscriptions.length;

      newIndex = Math.max(0, Math.min(newIndex, length - 1));

      newUserDto.currentSubscriptionIndex = newIndex;
      newUserDto.subscriptionId = newUserDto.subscriptions[newIndex].id;
    }

    localStorage.setItem(UserStoreKeys.accessToken, newUserDto.accessToken);

    // saving the userDto also in the local storage
    // because we would lose it otherwise on a full page reload
    localStorage.setItem(
      UserStoreKeys.loggedInUser,
      JSON.stringify(newUserDto)
    );
  }

  changeCurrentSubscription(newSubscriptionId: string) {
    return this.userService.changeCurrentSubscription(newSubscriptionId).pipe(
      mergeMap((userDto: UserDto) => {
        this.setLoggedInUser(userDto, true);
        this.stopRefreshTokenTimer();
        this.startRefreshTokenTimer(userDto.accessToken);

        return of(null);
      })
    );
  }

  reloadCurrentSubscription() {
    this.userService.reloadSubscription().subscribe((userDto) => {
      this.setLoggedInUser(userDto);
      this.stopRefreshTokenTimer();
      this.startRefreshTokenTimer(userDto.accessToken);
    });
  }

  logout() {
    this.stopRefreshTokenTimer();

    this.authService.revokeRefreshToken().subscribe(
      () => {
        this.removeAccessTokenAndGoToLogin();
      },
      () => {
        // in case of error
        this.removeAccessTokenAndGoToLogin();
      }
    );
  }

  logoutWithoutRevokingToken() {
    this.stopRefreshTokenTimer();
    this.removeAccessTokenAndGoToLogin();
  }

  private removeAccessTokenAndGoToLogin() {
    localStorage.clear();

    this.router.navigate([AppRoutes.login]);
  }

  private startRefreshTokenTimer(accessToken: string) {
    // parse json object from base64 encoded jwt token
    const jwtToken = JSON.parse(atob(accessToken.split('.')[1]));

    const expires = new Date(jwtToken.exp * 1000);
    const timeout = expires.getTime() - Date.now() - 60 * 1000;
    this.refreshTokenTimeout = setTimeout(
      () => this.getNewTokens().subscribe(),
      timeout
    );
  }

  private stopRefreshTokenTimer() {
    if (this.refreshTokenTimeout) {
      clearTimeout(this.refreshTokenTimeout);
    }

    this.refreshTokenTimeout = undefined;
  }

  private getNewTokens(): Observable<Response<UserDto>> {
    return this.authService.getNewTokens().pipe(
      map((response) => {
        this.saveUserInLocalStorage(response.dto);
        this.startRefreshTokenTimer(response.dto.accessToken);
        return response;
      })
    );
  }
}
