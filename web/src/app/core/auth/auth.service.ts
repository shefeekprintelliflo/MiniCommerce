import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, catchError, finalize, shareReplay, tap, throwError } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest, RegisterResponse } from './auth.models';

const TOKEN_KEY = 'minicommerce.accessToken';
const REFRESH_TOKEN_KEY = 'minicommerce.refreshToken';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  // Relative URL: the dev server proxies /api to the identity service (see proxy.conf.json).
  private readonly baseUrl = '/api/auth';

  // Shared while a refresh is running. The server treats a second use of an old refresh token as theft
  // and revokes every session, so concurrent 401s must wait on one refresh call instead of each sending their own.
  private refreshInFlight: Observable<AuthResponse> | null = null;

  // Drives the header's account icon. Starts from whatever token is already stored.
  readonly signedIn = signal(this.getToken() !== null);

  register(request: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(`${this.baseUrl}/register`, request);
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, request).pipe(
      tap((response) => this.storeTokens(response))
    );
  }

  // Exchanges the stored refresh token for a new pair. The old refresh token stops working immediately.
  refresh(): Observable<AuthResponse> {
    if (this.refreshInFlight) {
      return this.refreshInFlight;
    }

    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token is stored.'));
    }

    this.refreshInFlight = this.http.post<AuthResponse>(`${this.baseUrl}/refresh`, { refreshToken }).pipe(
      tap((response) => this.storeTokens(response)),
      catchError((error: unknown) => {
        // Only a rejected token ends the session. A network error should leave the user signed in.
        if (error instanceof HttpErrorResponse && error.status === 401) {
          this.clearSession();
        }
        return throwError(() => error);
      }),
      finalize(() => (this.refreshInFlight = null)),
      shareReplay(1)
    );

    return this.refreshInFlight;
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  logout(): void {
    const refreshToken = this.getRefreshToken();

    // Revoke on the server so the refresh token cannot be used again. The UI signs out either way.
    if (refreshToken) {
      this.http.post(`${this.baseUrl}/logout`, { refreshToken }).subscribe({ error: () => undefined });
    }

    this.clearSession();
  }

  private storeTokens(response: AuthResponse): void {
    localStorage.setItem(TOKEN_KEY, response.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, response.refreshToken);
    this.signedIn.set(true);
  }

  private clearSession(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    this.signedIn.set(false);
  }
}
