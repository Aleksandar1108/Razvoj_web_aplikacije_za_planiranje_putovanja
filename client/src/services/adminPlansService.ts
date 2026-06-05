import { getTravelPlansApiBaseUrl } from '../config/env';
import type { AdminCreateTravelPlanRequest, AdminTravelPlanRow } from '../models/adminPlan';
import type { TravelPlan } from '../models/travelPlan';
import { ApiError, apiRequestWithBase } from './httpClient';

function requireBase(): string {
  const b = getTravelPlansApiBaseUrl();
  if (!b) {
    throw new ApiError('Nije podešen VITE_API_BASE_URL u .env fajlu (ApiGateway).', 0);
  }
  return b;
}

export const adminPlansService = {
  async listPlans(accessToken: string | null): Promise<AdminTravelPlanRow[]> {
    const base = requireBase();
    return apiRequestWithBase<AdminTravelPlanRow[]>(
      base,
      '/api/v1/admin/travel-plans',
      { method: 'GET' },
      accessToken
    );
  },

  async createPlan(
    accessToken: string | null,
    body: AdminCreateTravelPlanRequest
  ): Promise<TravelPlan> {
    const base = requireBase();
    return apiRequestWithBase<TravelPlan>(
      base,
      '/api/v1/admin/travel-plans',
      { method: 'POST', body: JSON.stringify(body) },
      accessToken
    );
  },
};
