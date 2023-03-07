import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ShoppingService } from './shopping.service';

@Injectable({
  providedIn: 'root',
})
export class PublicShoppingService extends ShoppingService {
  protected type: string = 'public';

  constructor(httpClient: HttpClient) {
    super(httpClient);
  }
}
