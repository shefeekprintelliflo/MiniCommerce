// Mirrors Microservice.identity/Models/AuthDtos.cs. ASP.NET serializes these as camelCase.

export interface RegisterRequest {
  email: string;
  password: string;
}

export interface RegisterResponse {
  id: string;
  email: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  expiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
}

// Used by both /refresh and /logout.
export interface RefreshRequest {
  refreshToken: string;
}
