import { getTravelPlansApiBaseUrl } from '../config/env';
import { ApiError, apiRequestWithBase } from './httpClient';
import type { TravelPlan, TravelPlanUpsert } from '../models/travelPlan';

const basePath = '/api/v1/travel-plans';

function requireBase(): string {
  const b = getTravelPlansApiBaseUrl();
  if (!b) {
    throw new ApiError('Nije podešen VITE_TRAVEL_PLANS_API_BASE_URL u .env fajlu.', 0);
  }
  return b;
}

export async function listTravelPlans(accessToken: string | null): Promise<TravelPlan[]> {
  const base = requireBase();
  return apiRequestWithBase<TravelPlan[]>(base, basePath, { method: 'GET' }, accessToken);
}

export async function getTravelPlan(id: string, accessToken: string | null): Promise<TravelPlan> {
  const base = requireBase();
  return apiRequestWithBase<TravelPlan>(base, `${basePath}/${encodeURIComponent(id)}`, { method: 'GET' }, accessToken);
}

export async function createTravelPlan(body: TravelPlanUpsert, accessToken: string | null): Promise<TravelPlan> {
  const base = requireBase();
  return apiRequestWithBase<TravelPlan>(base, basePath, { method: 'POST', body: JSON.stringify(body) }, accessToken);
}

export async function updateTravelPlan(
  id: string,
  body: TravelPlanUpsert,
  accessToken: string | null
): Promise<TravelPlan> {
  const base = requireBase();
  return apiRequestWithBase<TravelPlan>(
    base,
    `${basePath}/${encodeURIComponent(id)}`,
    { method: 'PUT', body: JSON.stringify(body) },
    accessToken
  );
}

export async function deleteTravelPlan(id: string, accessToken: string | null): Promise<void> {
  const base = requireBase();
  await apiRequestWithBase<unknown>(
    base,
    `${basePath}/${encodeURIComponent(id)}`,
    { method: 'DELETE' },
    accessToken
  );
}
