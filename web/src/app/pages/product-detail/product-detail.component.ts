import { DecimalPipe, NgIf } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CartService } from '../../core/cart/cart.service';
import { ProductResponse } from '../../core/products/products.models';
import { ProductsService } from '../../core/products/products.service';
import { HeaderComponent } from '../../shared/header/header.component';

@Component({
  selector: 'app-product-detail',
  imports: [NgIf, RouterLink, DecimalPipe, HeaderComponent],
  templateUrl: './product-detail.component.html',
  styleUrl: './product-detail.component.css',
})
export class ProductDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly products = inject(ProductsService);
  protected readonly cart = inject(CartService);

  protected readonly product = signal<ProductResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);

  ngOnInit(): void {
    // The :id segment of /products/:id. The snapshot is enough because this page is reached by its own URL.
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.load(id);
  }

  private load(id: string): void {
    this.loading.set(true);
    this.notFound.set(false);

    this.products.list().subscribe({
      next: (items) => {
        const match = items.find((item) => item.id === id) ?? null;
        this.product.set(match);
        this.notFound.set(match === null);
        this.loading.set(false);
      },
      error: () => {
        this.product.set(null);
        this.notFound.set(true);
        this.loading.set(false);
      },
    });
  }
}
