import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ProductResponse } from './products.models';

@Injectable({ providedIn: 'root' })
export class ProductsService {
  private readonly http = inject(HttpClient);

  // Relative URL: the dev server proxies /api/products to the products service (see proxy.conf.json).
  private readonly baseUrl = '/api/products';

  list(): Observable<ProductResponse[]> {
    return this.http.get<ProductResponse[]>(this.baseUrl);
  }
}
