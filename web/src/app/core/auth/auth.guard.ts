import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

// Lets signed-in users match the route. Anyone else is sent to the sign-in page before the route is chosen.
export const authGuard: CanMatchFn = () => {
  const auth = inject(AuthService);
  return auth.signedIn() ? true : inject(Router).createUrlTree(['/login']);
};
