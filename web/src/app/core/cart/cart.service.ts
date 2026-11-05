import { HttpClient } from '@angular/common/http';
import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { Observable, Subject, catchError, concatMap, map, of } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { ProductResponse } from '../products/products.models';

// One line in the cart. Name and price are a snapshot taken when the shopper added the product.
export interface CartLine {
  productId: string;
  name: string;
  unitPrice: number;
  quantity: number;
}

// Mirrors Microservice.Cart/Models/CartDtos.cs.
interface CartResponse {
  lines: CartLine[];
  itemCount: number;
  total: number;
}

// The cart is stored on the server (Microservice.Cart). This service keeps a copy so the UI updates straight away.
// Changes are shown at once, then sent one at a time, so the server applies them in the order the shopper made them.
@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  // Relative URL: the dev server proxies /api/cart to the cart service (see proxy.conf.json).
  private readonly baseUrl = '/api/cart';

  readonly lines = signal<CartLine[]>([]);

  // True after a save or reload fails, until the next one succeeds. The cart page shows a warning while it is set.
  readonly syncError = signal(false);

  readonly itemCount = computed(() => this.lines().reduce((count, line) => count + line.quantity, 0));

  readonly total = computed(() => this.lines().reduce((sum, line) => sum + line.unitPrice * line.quantity, 0));

  // Changes that were made locally and not yet confirmed by the server. The server's copy replaces ours only when this is zero.
  private pending = 0;
  private needsReload = false;
  private readonly queue = new Subject<() => Observable<CartResponse>>();

  constructor() {
    this.queue
      .pipe(
        concatMap((send) =>
          send().pipe(
            map((cart) => ({ cart })),
            catchError(() => of({ cart: null }))
          )
        )
      )
      .subscribe(({ cart }) => {
        this.pending--;
        if (cart) {
          this.syncError.set(false);
          if (this.pending === 0) {
            this.lines.set(cart.lines);
          }
        } else {
          this.syncError.set(true);
          this.needsReload = true;
        }

        if (this.pending === 0 && this.needsReload) {
          this.needsReload = false;
          this.refresh();
        }
      });

    // Load the cart on sign-in, and empty it on sign-out so the next shopper never sees it.
    effect(() => {
      if (this.auth.signedIn()) {
        this.refresh();
      } else {
        this.lines.set([]);
        this.syncError.set(false);
      }
    });
  }

  // Replaces the local copy with the server's cart. Call it after a change this service did not make, such as checkout.
  refresh(): void {
    if (!this.auth.signedIn()) {
      return;
    }

    this.http.get<CartResponse>(this.baseUrl).subscribe({
      next: (cart) => {
        this.lines.set(cart.lines);
        this.syncError.set(false);
      },
      error: () => this.syncError.set(true),
    });
  }

  quantityOf(productId: string): number {
    return this.lines().find((line) => line.productId === productId)?.quantity ?? 0;
  }

  add(product: ProductResponse, quantity: number): void {
    this.setLine(this.snapshotOf(product, this.quantityOf(product.id) + quantity));
  }

  remove(product: ProductResponse, quantity: number): void {
    this.setLine(this.snapshotOf(product, Math.max(0, this.quantityOf(product.id) - quantity)));
  }

  // Sets the absolute quantity of a line already in the cart. 0 removes it.
  setLineQuantity(line: CartLine, quantity: number): void {
    this.setLine({ ...line, quantity: Math.max(0, quantity) });
  }

  clear(): void {
    this.lines.set([]);
    this.enqueue(() => this.http.delete<CartResponse>(this.baseUrl));
  }

  private snapshotOf(product: ProductResponse, quantity: number): CartLine {
    return { productId: product.id, name: product.name, unitPrice: product.price, quantity };
  }

  // Sends the absolute quantity, not the change. A late or repeated request then still leaves the cart in the right state.
  private setLine(line: CartLine): void {
    this.lines.update((lines) => {
      if (line.quantity === 0) {
        return lines.filter((existing) => existing.productId !== line.productId);
      }
      return lines.some((existing) => existing.productId === line.productId)
        ? lines.map((existing) => (existing.productId === line.productId ? line : existing))
        : [...lines, line];
    });

    this.enqueue(() =>
      this.http.put<CartResponse>(`${this.baseUrl}/items/${line.productId}`, {
        name: line.name,
        unitPrice: line.unitPrice,
        quantity: line.quantity,
      })
    );
  }

  private enqueue(send: () => Observable<CartResponse>): void {
    this.pending++;
    this.queue.next(send);
  }
}
