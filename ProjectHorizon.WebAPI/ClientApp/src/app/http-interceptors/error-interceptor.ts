import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { UserStore } from '../services/user.store';
import { Router } from '@angular/router';
import { AppRoutes } from '../constants/app-routes';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  constructor(private userStore: UserStore, private readonly router: Router) {}

  intercept(
    request: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    return next.handle(request).pipe(
      catchError((err) => {
        if ([401].includes(err.status)) {
          // auto logout if 401 response returned from api
          this.userStore.logoutWithoutRevokingToken();
        }

        if ([403].includes(err.status)) {
          if (this.router.url == AppRoutes.root) {
            this.userStore.logoutWithoutRevokingToken();
          } else {
            this.router.navigate([AppRoutes.root]);
          }
        }

        return throwError(err);
      })
    );
  }
}
