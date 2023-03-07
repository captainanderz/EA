import { Injectable } from '@angular/core';
import {
  CanActivate,
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
  UrlTree,
  Router,
} from '@angular/router';
import { Observable, of } from 'rxjs';
import { catchError, mergeMap } from 'rxjs/operators';
import { AppRoutes } from '../constants/app-routes';
import { TermsAndConditionsService } from '../services/terms-and-conditions.service';

@Injectable({
  providedIn: 'root',
})
export class TermsGuard implements CanActivate {
  constructor(
    private readonly termsAndConditionsService: TermsAndConditionsService,
    private readonly router: Router
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ):
    | Observable<boolean | UrlTree>
    | Promise<boolean | UrlTree>
    | boolean
    | UrlTree {
    return this.termsAndConditionsService.checkAccepted().pipe(
      catchError((error) => of(true)),
      mergeMap((accepted, _) => {
        if (!accepted) {
          this.router.navigate([AppRoutes.terms]);
        }

        return of(accepted);
      })
    );
  }
}
