import { getActivitiesApiBaseUrl } from '../config/env';
import { ApiError, apiRequestWithBase } from './httpClient';
import type { TravelActivity, TravelActivityUpsert } from '../models/travelActivity';

function basePath(planId: string): string {
  return `/api/v1/travel-plans/${encodeURIComponent(planId)}/activities`;
}

function requireBase(): string {
  const b = getActivitiesApiBaseUrl();
  if (!b) {
    throw new ApiError('Nije podešen VITE_ACTIVITIES_API_BASE_URL u .env fajlu.', 0);
  }
  return b;
}

export async function listActivities(
  planId: string,
  accessToken: string | null,
  shareToken?: string | null
): Promise<TravelActivity[]> {
  const base = requireBase();
  return apiRequestWithBase<TravelActivity[]>(base, basePath(planId), { method: 'GET' }, accessToken, shareToken ?? null);
}

export async function getActivity(
  planId: string,
  activityId: string,
  accessToken: string | null,
  shareToken?: string | null
): Promise<TravelActivity> {
  const base = requireBase();
  return apiRequestWithBase<TravelActivity>(
    base,
    `${basePath(planId)}/${encodeURIComponent(activityId)}`,
    { method: 'GET' },
    accessToken,
    shareToken ?? null
  );
}

export async function createActivity(
  planId: string,
  body: TravelActivityUpsert,
  accessToken: string | null,
  shareToken?: string | null
): Promise<TravelActivity> {
  const base = requireBase();
  return apiRequestWithBase<TravelActivity>(
    base,
    basePath(planId),
    { method: 'POST', body: JSON.stringify(body) },
    accessToken,
    shareToken ?? null
  );
}

export async function updateActivity(
  planId: string,
  activityId: string,
  body: TravelActivityUpsert,
  accessToken: string | null,
  shareToken?: string | null
): Promise<TravelActivity> {
  const base = requireBase();
  return apiRequestWithBase<TravelActivity>(
    base,
    `${basePath(planId)}/${encodeURIComponent(activityId)}`,
    { method: 'PUT', body: JSON.stringify(body) },
    accessToken,
    shareToken ?? null
  );
}

export async function deleteActivity(
  planId: string,
  activityId: string,
  accessToken: string | null,
  shareToken?: string | null
): Promise<void> {
  const base = requireBase();
  await apiRequestWithBase<unknown>(
    base,
    `${basePath(planId)}/${encodeURIComponent(activityId)}`,
    { method: 'DELETE' },
    accessToken,
    shareToken ?? null
  );
}
