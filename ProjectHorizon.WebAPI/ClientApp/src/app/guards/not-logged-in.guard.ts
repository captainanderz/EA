import { Injectable } from '@angular/core';
import {
  CanActivate,
  CanActivateChild,
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
  UrlTree,
  Router,
} from '@angular/router';
import { JwtHelperService } from '@auth0/angular-jwt';
import { mergeMap } from 'rxjs/operators';
import { Observable, of } from 'rxjs';
import { AppRoutes } from '../constants/app-routes';
import { SubscriptionState } from '../constants/subscription-state';
import { UserStoreKeys } from '../constants/user-store-keys';
import { UserDto } from '../dtos/user-dto.model';
import { ApplicationInformationService } from '../services/application-information.service';

@Injectable({
  providedIn: 'root',
})
export class NotLoggedInGuard implements CanActivate, CanActivateChild {
  constructor(
    private readonly jwtHelperService: JwtHelperService,
    private readonly router: Router,
    private readonly applicationInformationService: ApplicationInformationService
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ):
    | Observable<boolean | UrlTree>
    | Promise<boolean | UrlTree>
    | boolean
    | UrlTree {
    const token = localStorage.getItem(UserStoreKeys.accessToken);

    if (!token || this.jwtHelperService.isTokenExpired(token)) {
      return true;
    }

    const decodedToken = this.jwtHelperService.decodeToken(token);
    if (!decodedToken) {
      return true;
    }

    const mfaValue = decodedToken['Mfa'];
    if (mfaValue == 'NeedToEnterCode') {
      return true;
    }

    if (mfaValue == 'NeedToConfigure') {
      return true;
    }

    var loggedInUser = localStorage.getItem(UserStoreKeys.loggedInUser);

    if (loggedInUser) {
      let userDto = JSON.parse(loggedInUser) as UserDto;
      if (
        userDto.subscriptions[userDto.currentSubscriptionIndex].state ===
        SubscriptionState.PaymentNotSetUp
      ) {
        return this.applicationInformationService.get().pipe(
          mergeMap((information, _) => {
            if (information.askForPaymentAfterRegister) {
              return of(true);
            }

            this.router.navigate([AppRoutes.root]);

            return of(false);
          })
        );
      }
    }

    // check if the list of allowed roles is empty, if empty, authorize the user to access the page
    const allowedRoles: string[] | undefined = route.data.allowedRoles;
    if (allowedRoles == undefined || allowedRoles.length === 0) {
      this.router.navigate([AppRoutes.root]);
      return false;
    }

    // check if the user role is in the list of allowed roles, return true if allowed and false if not allowed
    const userRole: string = decodedToken['role'];
    if (!allowedRoles.includes(userRole)) {
      return true;
    }

    this.router.navigate([AppRoutes.root]);

    return false;
  }

  canActivateChild(
    childRoute: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ):
    | Observable<boolean | UrlTree>
    | Promise<boolean | UrlTree>
    | boolean
    | UrlTree {
    return true;
  }
}
