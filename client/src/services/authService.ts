import { getAuthApiBaseUrl } from '../config/env';
import type { AuthResponse, AuthUser, LoginRequest, RegisterRequest } from '../models/auth';
import { ApiError, apiRequestWithBase } from './httpClient';

function requireAuthBase(): string {
  const base = getAuthApiBaseUrl();
  if (!base) {
    throw new ApiError(
      'Nije podešen URL za autentikaciju: postavi VITE_AUTH_API_BASE_URL ili VITE_API_BASE_URL u .env.',
      0
    );
  }
  return base;
}

/**
 * HTTP pozivi za autentikaciju. Komponente ne zovu `fetch` direktno — koriste `AuthContext`,
 * koji interno koristi ovaj servis.
 */
export const authService = {
  async register(payload: RegisterRequest): Promise<AuthResponse> {
    const base = requireAuthBase();
    return apiRequestWithBase<AuthResponse>(base, '/api/v1/auth/register', {
      method: 'POST',
      body: JSON.stringify(payload),
    });
  },

  async login(payload: LoginRequest): Promise<AuthResponse> {
    const base = requireAuthBase();
    return apiRequestWithBase<AuthResponse>(base, '/api/v1/auth/login', {
      method: 'POST',
      body: JSON.stringify(payload),
    });
  },

  async getMe(accessToken: string): Promise<AuthUser> {
    const base = requireAuthBase();
    return apiRequestWithBase<AuthUser>(base, '/api/v1/auth/me', { method: 'GET' }, accessToken);
  },
};
