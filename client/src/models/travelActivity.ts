export const ACTIVITY_STATUSES = ['planned', 'reserved', 'completed', 'cancelled'] as const;
export type TravelActivityStatus = (typeof ACTIVITY_STATUSES)[number];

export type TravelActivity = {
  id: string;
  travelPlanId: string;
  name: string;
  activityDate: string;
  activityTime: string;
  location: string;
  description: string | null;
  estimatedCost: number;
  status: TravelActivityStatus;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type TravelActivityUpsert = {
  name: string;
  activityDate: string;
  activityTime: string;
  location: string;
  description: string | null;
  estimatedCost: number;
  status: TravelActivityStatus;
};
