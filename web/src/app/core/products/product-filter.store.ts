import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { ProductFilters } from '../../shared/product-filter/product-filter.component';

// Keeps the product list filters for the whole session. The home page reads the last value when it opens,
// so the shopper's filters are still there after visiting a product and coming back.
@Injectable({ providedIn: 'root' })
export class ProductFilterStore {
  private readonly filtersSubject = new BehaviorSubject<ProductFilters>({
    search: '',
    inStockOnly: false,
    outOfStockOnly: false,
  });

  // Emits the current value straight away to each new subscriber, then every change.
  readonly filters$: Observable<ProductFilters> = this.filtersSubject.asObservable();

  // The latest value without subscribing. Used where a one-off read is enough.
  get current(): ProductFilters {
    return this.filtersSubject.value;
  }

  set(filters: ProductFilters): void {
    this.filtersSubject.next(filters);
  }
}
