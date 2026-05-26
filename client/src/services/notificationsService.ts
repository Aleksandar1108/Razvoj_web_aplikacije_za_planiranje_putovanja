import type { UnreadNotificationCount, UserNotification } from '../models/notification';
import { apiRequest } from './httpClient';

export const notificationsService = {
  async list(accessToken: string | null): Promise<UserNotification[]> {
    return apiRequest<UserNotification[]>('/api/v1/notifications', { method: 'GET' }, accessToken);
  },

  async unreadCount(accessToken: string | null): Promise<UnreadNotificationCount> {
    return apiRequest<UnreadNotificationCount>(
      '/api/v1/notifications/unread-count',
      { method: 'GET' },
      accessToken
    );
  },

  async markRead(accessToken: string | null, notificationId: string): Promise<void> {
    await apiRequest<unknown>(
      `/api/v1/notifications/${encodeURIComponent(notificationId)}/read`,
      { method: 'PATCH' },
      accessToken
    );
  },

  async markAllRead(accessToken: string | null): Promise<void> {
    await apiRequest<unknown>('/api/v1/notifications/read-all', { method: 'PATCH' }, accessToken);
  },
};
