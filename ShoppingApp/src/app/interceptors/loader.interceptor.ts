import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { ApiRoutes } from '../constants/api-routes';
import { LoaderService } from '../services/loader.service';

@Injectable()
export class LoaderInterceptor implements HttpInterceptor {
  // Fields
  private count = 0;
  private readonly ignoredPaths = [new RegExp(ApiRoutes.login)];

  // Constructor
  constructor(private readonly loaderService: LoaderService) {}

  // Methods
  intercept(
    req: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    console.log(req.urlWithParams);
    if (this.ignoredPaths.some((path) => path.test(req.urlWithParams))) {
      return next.handle(req);
    }

    if (this.count === 0) {
      this.loaderService.setHttpProgressStatus(true);
    }

    this.count++;

    return next.handle(req).pipe(
      finalize(() => {
        this.count--;
        if (this.count === 0) {
          this.loaderService.setHttpProgressStatus(false);
        }
      })
    );
  }
}
