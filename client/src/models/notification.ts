export interface UserNotification {
  id: string;
  category: string;
  title: string;
  message: string;
  travelPlanId: string | null;
  travelDestinationId: string | null;
  isRead: boolean;
  createdAtUtc: string;
}

export interface UnreadNotificationCount {
  count: number;
}
