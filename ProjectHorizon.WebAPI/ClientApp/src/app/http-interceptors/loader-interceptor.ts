import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { LoaderService } from '../services/loader.service';

@Injectable()
export class LoaderInterceptor implements HttpInterceptor {
  private count = 0;
  private readonly ignoredPaths = [
    //TODO check d+
    /api\/public-applications\/upload/,
    /api\/public-applications\/add/,
    /api\/public-applications\/\d+\/deploy/,
    /api\/private-applications\/upload/,
    /api\/private-applications\/add/,
    /api\/private-applications\/\d+\/deploy/,
    /api\/subscriptions\/filter/,
    /api\/notifications\/recent/,
    /api\/approvals\/count/,
    /api\/shopping\/requests\/count/,
    /api\/notifications\/mark-all-as-read/,
    /api\/private-applications\/\d+\/deploy/,
    /api\/auth\/new-tokens/,
    /api\/terms\/check-accepted/,
    /api\/application-information/,
    /api\/private-applications\/\d+\/download-uri/,
    /api\/public-applications\/\d+\/download-uri/,
  ];

  constructor(private readonly loaderService: LoaderService) {}

  intercept(
    req: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    if (this.ignoredPaths.some((path) => path.test(req.urlWithParams)))
      return next.handle(req);

    if (this.count === 0) this.loaderService.setHttpProgressStatus(true);
    this.count++;

    return next.handle(req).pipe(
      finalize(() => {
        this.count--;
        if (this.count === 0) this.loaderService.setHttpProgressStatus(false);
      })
    );
  }
}
