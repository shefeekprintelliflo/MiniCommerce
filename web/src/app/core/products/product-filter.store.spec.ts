import { TestBed } from '@angular/core/testing';
import { ProductFilterStore } from './product-filter.store';

describe('ProductFilterStore', () => {
  let store: ProductFilterStore;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    store = TestBed.inject(ProductFilterStore);
  });

  it('starts with no filters applied', () => {
    expect(store.current).toEqual({ search: '', inStockOnly: false, outOfStockOnly: false });
  });

  it('gives the latest value to a new subscriber', () => {
    store.set({ search: 'shirt', inStockOnly: true, outOfStockOnly: false });

    let received: unknown;
    store.filters$.subscribe((value) => (received = value));

    expect(received).toEqual({ search: 'shirt', inStockOnly: true, outOfStockOnly: false });
  });

  it('emits every change to existing subscribers', () => {
    const seen: string[] = [];
    store.filters$.subscribe((value) => seen.push(value.search));

    store.set({ search: 'a', inStockOnly: false, outOfStockOnly: false });
    store.set({ search: 'ab', inStockOnly: false, outOfStockOnly: false });

    expect(seen).toEqual(['', 'a', 'ab']);
  });
});
