import { Injectable } from '@angular/core';
import {
  ActivatedRouteSnapshot,
  CanActivate,
  Router,
  RouterStateSnapshot,
  UrlTree,
} from '@angular/router';
import { MsalService } from '@azure/msal-angular';
import { Observable } from 'rxjs';
import { AppRoutes } from '../constants/app-routes';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root',
})
export class AuthGuard implements CanActivate {
  // Constructor
  constructor(
    private router: Router,
    private authService: AuthService,
    private msalService: MsalService
  ) {}

  // Methods
  // Checks if the user that logs in has an account
  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ):
    | Observable<boolean | UrlTree>
    | Promise<boolean | UrlTree>
    | boolean
    | UrlTree {
    if (this.msalService.instance.getAllAccounts().length == 0) {
      this.router.navigate([AppRoutes.login]);
      return false;
    }

    if (!this.authService.hasAccessToken()) {
      this.router.navigate([AppRoutes.subscriptionCheck]);
      return false;
    }

    return true;
  }
}
