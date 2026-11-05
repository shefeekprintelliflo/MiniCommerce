import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { checkoutLeaveGuard } from './core/checkout/checkout-leave.guard';

// Each page is loaded only when its route is visited, so signed-out users never download the signed-in pages.
// Guards use canMatch: it runs before the lazy chunk is fetched. canActivate would download the code first.
export const routes: Routes = [
  {
    path: 'cart',
    loadComponent: () => import('./pages/cart/cart.component').then((m) => m.CartComponent),
    canMatch: [authGuard],
  },
  {
    path: 'checkout',
    loadComponent: () => import('./pages/checkout/checkout.component').then((m) => m.CheckoutComponent),
    canMatch: [authGuard],
    canDeactivate: [checkoutLeaveGuard],
  },
  {
    path: 'products/:id',
    loadComponent: () =>
      import('./pages/product-detail/product-detail.component').then((m) => m.ProductDetailComponent),
    canMatch: [authGuard],
  },
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'home',
    loadComponent: () => import('./pages/home/home.component').then((m) => m.HomeComponent),
    canMatch: [authGuard],
  },
  {
    path: 'register',
    loadComponent: () => import('./pages/register/register.component').then((m) => m.RegisterComponent),
  },
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: '**', redirectTo: 'login' }
];
