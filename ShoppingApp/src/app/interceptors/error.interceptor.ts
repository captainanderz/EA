import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
} from '@angular/common/http';
import {
  BehaviorSubject,
  catchError,
  filter,
  Observable,
  switchMap,
  take,
  throwError,
} from 'rxjs';
import { AuthService } from '../services/auth.service';
import { ApiRoutes } from '../constants/api-routes';
import { UserStoreKeys } from '../constants/user-store-keys';

/**
 * Catches request errors with the Unauthorized status and tries to log in again, before retrying the request.
 */
@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  // Fields
  private refreshTokenInProgress = false;
  private refreshTokenSubject: BehaviorSubject<boolean> =
    new BehaviorSubject<boolean>(false);

  private readonly ignoredPaths = [new RegExp(ApiRoutes.login)];

  // Constructor
  constructor(private readonly authService: AuthService) {}

  // Methods
  intercept(
    request: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    if (this.ignoredPaths.some((path) => path.test(request.urlWithParams))) {
      return next.handle(request);
    }

    return next.handle(request).pipe(
      catchError((error) => {
        // throw errors other than 401
        if (error.status != 401) {
          return throwError(() => error);
        }

        // if the token refresh is in progress wait until it finishes
        if (this.refreshTokenInProgress) {
          return this.refreshTokenSubject.pipe(
            filter((inProgress) => !inProgress),
            take(1),
            switchMap(() =>
              next.handle(
                request.clone({
                  setHeaders: {
                    Authorization: `Bearer ${localStorage.getItem(
                      UserStoreKeys.accessToken
                    )}`,
                  },
                })
              )
            )
          );
        }

        this.refreshTokenInProgress = true;
        this.refreshTokenSubject.next(true);

        // try to log in then retry the request
        return this.authService.login().pipe(
          switchMap(() => {
            this.refreshTokenInProgress = false;
            this.refreshTokenSubject.next(false);

            return next.handle(
              request.clone({
                setHeaders: {
                  Authorization: `Bearer ${localStorage.getItem(
                    UserStoreKeys.accessToken
                  )}`,
                },
              })
            );
          }),
          catchError((err) => {
            // if there's an error when trying to log in again, log out completely and throw the error
            this.refreshTokenInProgress = false;

            // this.authService.logout();
            return throwError(() => err);
          })
        );
      })
    );
  }
}
