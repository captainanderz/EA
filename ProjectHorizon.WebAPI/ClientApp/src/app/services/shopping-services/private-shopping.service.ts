import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ShoppingService } from './shopping.service';

@Injectable({
  providedIn: 'root',
})
export class PrivateShoppingService extends ShoppingService {
  protected type: string = 'private';

  constructor(httpClient: HttpClient) {
    super(httpClient);
  }
}
