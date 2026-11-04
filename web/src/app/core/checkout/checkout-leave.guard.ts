import { CanDeactivateFn } from '@angular/router';
import type { CheckoutComponent } from '../../pages/checkout/checkout.component';

// Asks before leaving checkout while there are items in the cart, so the shopper doesn't leave by accident.
export const checkoutLeaveGuard: CanDeactivateFn<CheckoutComponent> = (component) => {
  if (component.cart.itemCount() === 0) {
    return true;
  }
  return window.confirm('You still have items in your cart. Leave checkout?');
};
