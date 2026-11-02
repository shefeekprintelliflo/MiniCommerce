import { HttpErrorResponse } from '@angular/common/http';
import { NgFor, NgIf } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { ProductResponse } from '../../core/products/products.models';
import { ProductsService } from '../../core/products/products.service';
import { HeaderComponent } from '../../shared/header/header.component';
import { ProductCardComponent } from '../../shared/product-card/product-card.component';

@Component({
  selector: 'app-home',
  imports: [NgIf, NgFor, RouterLink, HeaderComponent, ProductCardComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css',
})
export class HomeComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly products = inject(ProductsService);

  protected readonly signedIn = this.auth.signedIn;
  protected readonly loading = signal(false);
  protected readonly items = signal<ProductResponse[]>([]);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    // The products API requires a signed-in user, so only load the list when there is a token.
    if (this.signedIn()) {
      this.load();
    }
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
