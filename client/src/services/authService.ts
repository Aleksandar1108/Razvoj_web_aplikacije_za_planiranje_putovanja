import type { AuthResponse, AuthUser, LoginRequest, RegisterRequest } from '../models/auth';
import { apiRequest } from './httpClient';

/**
 * HTTP pozivi za autentikaciju. Komponente ne zovu `fetch` direktno — koriste `AuthContext`,
 * koji interno koristi ovaj servis.
 */
export const authService = {
  async register(payload: RegisterRequest): Promise<AuthResponse> {
    return apiRequest<AuthResponse>('/api/v1/auth/register', {
      method: 'POST',
      body: JSON.stringify(payload),
    });
  },

  async login(payload: LoginRequest): Promise<AuthResponse> {
    return apiRequest<AuthResponse>('/api/v1/auth/login', {
      method: 'POST',
      body: JSON.stringify(payload),
    });
  },

  async getMe(accessToken: string): Promise<AuthUser> {
    const res = await apiRequest<AuthUser>('/api/v1/auth/me', { method: 'GET' }, accessToken);
    return res;
  },
};
