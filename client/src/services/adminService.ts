import type { AdminSystemStats, AdminUserRow, UpdateAdminUserRequest } from '../models/admin';
import { apiRequest } from './httpClient';

export const adminService = {
  async getStats(accessToken: string | null): Promise<AdminSystemStats> {
    return apiRequest<AdminSystemStats>('/api/v1/admin/stats', { method: 'GET' }, accessToken);
  },

  async listUsers(accessToken: string | null): Promise<AdminUserRow[]> {
    return apiRequest<AdminUserRow[]>('/api/v1/admin/users', { method: 'GET' }, accessToken);
  },

  async updateUser(
    accessToken: string | null,
    userId: string,
    body: UpdateAdminUserRequest
  ): Promise<AdminUserRow> {
    return apiRequest<AdminUserRow>(
      `/api/v1/admin/users/${encodeURIComponent(userId)}`,
      { method: 'PATCH', body: JSON.stringify(body) },
      accessToken
    );
  },
};
