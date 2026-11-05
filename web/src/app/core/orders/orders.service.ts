import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { OrderResponse } from './orders.models';

@Injectable({ providedIn: 'root' })
export class OrdersService {
  private readonly http = inject(HttpClient);

  // Relative URL: the dev server proxies /api/orders to the orders service (see proxy.conf.json).
  private readonly baseUrl = '/api/orders';

  // The server reads the cart, reserves stock, and saves the order. A 409 means the order was refused and carries a message.
  placeOrder(): Observable<OrderResponse> {
    return this.http.post<OrderResponse>(this.baseUrl, {});
  }
}
