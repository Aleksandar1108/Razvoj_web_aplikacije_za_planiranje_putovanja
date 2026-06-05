import { getDestinationsApiBaseUrl } from '../config/env';
import type { AdminDestinationRow, AdminTravelPlanOption } from '../models/adminDestination';
import { ApiError, apiRequestWithBase } from './httpClient';

function requireBase(): string {
  const b = getDestinationsApiBaseUrl();
  if (!b) {
    throw new ApiError('Nije podešen VITE_API_BASE_URL u .env fajlu (ApiGateway).', 0);
  }
  return b;
}

export const adminDestinationsService = {
  async listDestinations(accessToken: string | null): Promise<AdminDestinationRow[]> {
    const base = requireBase();
    return apiRequestWithBase<AdminDestinationRow[]>(
      base,
      '/api/v1/admin/destinations',
      { method: 'GET' },
      accessToken
    );
  },

  async listTravelPlans(accessToken: string | null): Promise<AdminTravelPlanOption[]> {
    const base = requireBase();
    return apiRequestWithBase<AdminTravelPlanOption[]>(
      base,
      '/api/v1/admin/travel-plans',
      { method: 'GET' },
      accessToken
    );
  },
};
