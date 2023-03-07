import { Injectable } from '@angular/core';
import {
  ActivatedRouteSnapshot,
  CanActivate,
  CanActivateChild,
  Router,
  RouterStateSnapshot,
  UrlTree,
} from '@angular/router';
import { JwtHelperService } from '@auth0/angular-jwt';
import { Observable, of } from 'rxjs';
import { mergeMap } from 'rxjs/operators';
import { AppRoutes } from '../constants/app-routes';
import { SubscriptionState } from '../constants/subscription-state';
import { UserStoreKeys } from '../constants/user-store-keys';
import { UserDto } from '../dtos/user-dto.model';
import { ApplicationInformationService } from '../services/application-information.service';
import { UserStore } from '../services/user.store';

@Injectable()
export class AuthGuard implements CanActivate, CanActivateChild {
  constructor(
    private readonly jwtHelperService: JwtHelperService,
    private readonly router: Router,
    private readonly applicationInformationService: ApplicationInformationService,
    private readonly userStore: UserStore
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ):
    | boolean
    | UrlTree
    | Observable<boolean | UrlTree>
    | Promise<boolean | UrlTree> {
    const token = localStorage.getItem(UserStoreKeys.accessToken);

    if (!token || this.jwtHelperService.isTokenExpired(token)) {
      this.userStore.logout();

      return false;
    }

    const decodedToken = this.jwtHelperService.decodeToken(token);
    if (!decodedToken) {
      this.router.navigate([AppRoutes.login]);
      return false;
    }

    const mfaValue = decodedToken['Mfa'];
    if (mfaValue == 'NeedToEnterCode') {
      this.router.navigate([AppRoutes.loginMfa], {
        queryParams: { returnUrl: state.url },
      });
      return false;
    }

    if (mfaValue == 'NeedToConfigure') {
      this.router.navigate([AppRoutes.signUpSetupAuthenticator], {
        queryParams: { returnUrl: state.url },
      });
      return false;
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
            if (!information.askForPaymentAfterRegister) {
              return of(true);
            }

            this.router.navigate([AppRoutes.paymentSetup]);

            return of(false);
          })
        );
      }
    }

    // check if the list of allowed roles is empty, if empty, authorize the user to access the page
    const allowedRoles: string[] | undefined = route.data.allowedRoles;
    if (allowedRoles == undefined || allowedRoles.length === 0) return true;

    // check if the user role is in the list of allowed roles, return true if allowed and false if not allowed
    const userRole: string = decodedToken['role'];
    if (!allowedRoles.includes(userRole)) {
      this.router.navigate([AppRoutes.root]);
      return false;
    }

    return true;
  }

  canActivateChild(
    childRoute: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ):
    | boolean
    | UrlTree
    | Observable<boolean | UrlTree>
    | Promise<boolean | UrlTree> {
    return this.canActivate(childRoute, state);
  }
}
