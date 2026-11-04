import { DecimalPipe, NgFor, NgIf } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { CartService } from '../../core/cart/cart.service';
import { HeaderComponent } from '../../shared/header/header.component';

@Component({
  selector: 'app-cart',
  imports: [NgIf, NgFor, RouterLink, RouterOutlet, DecimalPipe, HeaderComponent],
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.css',
})
export class CartComponent {
  protected readonly cart = inject(CartService);
  
}
