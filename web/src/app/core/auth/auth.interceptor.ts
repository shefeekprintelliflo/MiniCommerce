import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

// Adds the stored bearer token to outgoing requests. Requests made while signed out pass through unchanged.
// When the server rejects the token with 401, refreshes it once and retries the original request.
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.getToken();

  // The auth endpoints handle their own 401s (wrong password, bad refresh token), so they are never retried here.
  if (req.url.startsWith('/api/auth/')) {
    return next(req);
  }

  if (!token) {
    return next(req);
  }

  return next(withToken(req, token)).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      return auth.refresh().pipe(
        switchMap((response) => next(withToken(req, response.accessToken)))
      );
    })
  );
};

function withToken(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}
