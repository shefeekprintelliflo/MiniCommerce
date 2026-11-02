import { DecimalPipe, NgIf } from '@angular/common';
import { Component, input } from '@angular/core';
import { Card } from 'primeng/card';
import { ProductResponse } from '../../core/products/products.models';

@Component({
  selector: 'app-product-card',
  imports: [NgIf, DecimalPipe, Card],
  templateUrl: './product-card.component.html',
  styleUrl: './product-card.component.css',
})
export class ProductCardComponent {
  readonly product = input.required<ProductResponse>();
}
