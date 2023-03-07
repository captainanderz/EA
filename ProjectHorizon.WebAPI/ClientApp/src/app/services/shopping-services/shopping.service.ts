import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ShopAddDto } from 'src/app/dtos/shop-add-dto.model';
import { ShopRemoveDto } from 'src/app/dtos/shop-remove-dto';

export abstract class ShoppingService {
  protected abstract readonly type: string;
  private readonly shoppingUrl: string = `/api/shopping`;
  private readonly applicationsUrl: string = `${this.shoppingUrl}/applications`;

  constructor(private readonly httpClient: HttpClient) {}

  addApplications(dto: ShopAddDto): Observable<any> {
    return this.httpClient.post(`${this.applicationsUrl}/${this.type}`, dto);
  }

  removeApplications(dto: ShopRemoveDto): Observable<any> {
    return this.httpClient.request(
      'delete',
      `${this.applicationsUrl}/${this.type}`,
      { body: dto }
    );
  }
}
