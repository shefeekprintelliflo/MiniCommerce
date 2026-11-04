import { HttpErrorResponse } from '@angular/common/http';
import { NgFor, NgIf } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { CartService } from '../../core/cart/cart.service';
import { ProductResponse } from '../../core/products/products.models';
import { ProductsService } from '../../core/products/products.service';
import { CartSummaryComponent } from '../../shared/cart-summary/cart-summary.component';
import { HeaderComponent } from '../../shared/header/header.component';
import { ProductCardComponent } from '../../shared/product-card/product-card.component';
import { ProductFilterComponent, ProductFilters } from '../../shared/product-filter/product-filter.component';

@Component({
  selector: 'app-home',
  imports: [NgIf, NgFor, RouterLink, HeaderComponent, ProductCardComponent, ProductFilterComponent, CartSummaryComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css',
})
export class HomeComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly products = inject(ProductsService);
  protected readonly cart = inject(CartService);

  protected readonly signedIn = this.auth.signedIn;
  protected readonly loading = signal(false);
  protected readonly items = signal<ProductResponse[]>([]);
  protected readonly error = signal<string | null>(null);

  // Filter state lives here. The filter bar only displays it and reports changes.
  protected readonly filters = signal<ProductFilters>({ search: '', inStockOnly: false, outOfStockOnly: false });

  protected readonly visibleItems = computed(() => {
    const { search, inStockOnly, outOfStockOnly } = this.filters();
    const term = search.trim().toLowerCase();

    return this.items().filter((product) => {
      const matchesSearch =
        term === '' ||
        product.name.toLowerCase().includes(term) ||
        (product.description ?? '').toLowerCase().includes(term);
      const matchesStock = (!inStockOnly || product.stockQuantity > 0) && (!outOfStockOnly || product.stockQuantity === 0);
      return matchesSearch && matchesStock;
    });
  });

  ngOnInit(): void {
    // The products API requires a signed-in user, so only load the list when there is a token.
    if (this.signedIn()) {
      this.load();
    }
  }

  protected onFiltersChange(filters: ProductFilters): void {
    this.filters.set(filters);
  }

  protected onAddToCart({ product, quantity }: { product: ProductResponse; quantity: number }): void {
    this.cart.add(product, quantity);
  }

  protected onRemove({ product, quantity }: { product: ProductResponse; quantity: number }): void {
    this.cart.remove(product, quantity);
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.products.list().subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.error.set(
          error instanceof HttpErrorResponse && error.status === 401
            ? 'Your session has expired. Sign in again to see products.'
            : 'Products could not be loaded. Please try again.'
        );
      }
    });
  }
}
