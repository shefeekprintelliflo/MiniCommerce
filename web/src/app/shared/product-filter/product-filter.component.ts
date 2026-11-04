import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Checkbox } from 'primeng/checkbox';
import { IconField } from 'primeng/iconfield';
import { InputIcon } from 'primeng/inputicon';
import { InputText } from 'primeng/inputtext';

export interface ProductFilters {
  search: string;
  inStockOnly: boolean;
  outOfStockOnly: boolean;
}

@Component({
  selector: 'app-product-filter',
  imports: [FormsModule, Checkbox, IconField, InputIcon, InputText],
  templateUrl: './product-filter.component.html',
  styleUrl: './product-filter.component.css',
})
export class ProductFilterComponent {
  // Parent -> child: the current filter values, so the bar always shows what the list is using.
  readonly value = input.required<ProductFilters>(); 

  // Child -> parent: a new filter value after the shopper changes a control.
  readonly valueChange = output<ProductFilters>();

  protected onSearch(search: string): void {
    this.valueChange.emit({ ...this.value(), search });
  }

  protected onInStockChange(inStockOnly: boolean): void {
    this.valueChange.emit({ ...this.value(), inStockOnly, outOfStockOnly: this.value().outOfStockOnly && !inStockOnly });
  }

  protected onOutOfStockChange(outOfStockOnly: boolean): void {
    let inStockOnly = this.value().inStockOnly;
    if(inStockOnly && outOfStockOnly) {
      inStockOnly = false;
    }
    this.valueChange.emit({ ...this.value(), outOfStockOnly, inStockOnly });
  }
}
