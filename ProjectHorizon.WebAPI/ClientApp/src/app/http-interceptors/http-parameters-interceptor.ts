import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpParams,
  HttpRequest,
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpParameterEncoder } from './http-parameter-encoder';

@Injectable()
export class HttpParametersInterceptor implements HttpInterceptor {
  private static readonly Encoder = new HttpParameterEncoder();

  constructor() {}

  intercept(
    req: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    const params = new HttpParams({
      encoder: HttpParametersInterceptor.Encoder,
      fromString: req.params.toString(),
    });

    return next.handle(req.clone({ params }));
  }
}
