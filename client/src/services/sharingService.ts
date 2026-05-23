import { getSharingApiBaseUrl } from '../config/env';
import { ApiError, apiRequestWithBase } from './httpClient';
import type { CreateShareLinkResponse, SharedTravelPlanListItem, SharePermission } from '../models/share';

function requireBase(): string {
  const b = getSharingApiBaseUrl();
  if (!b) {
    throw new ApiError('Nije podešen VITE_SHARING_API_BASE_URL u .env fajlu.', 0);
  }
  return b;
}

export async function createShareLink(
  planId: string,
  permission: SharePermission,
  accessToken: string | null
): Promise<CreateShareLinkResponse> {
  const base = requireBase();
  return apiRequestWithBase<CreateShareLinkResponse>(
    base,
    `/api/v1/travel-plans/${encodeURIComponent(planId)}/share-links`,
    { method: 'POST', body: JSON.stringify({ permission }) },
    accessToken,
    null
  );
}

export async function listSharedPlans(accessToken: string | null): Promise<SharedTravelPlanListItem[]> {
  const base = requireBase();
  return apiRequestWithBase<SharedTravelPlanListItem[]>(base, '/api/v1/shared-plans', { method: 'GET' }, accessToken, null);
}

export async function claimShareLink(token: string, accessToken: string | null): Promise<{ travelPlanId: string; permission: SharePermission }> {
  const base = requireBase();
  return apiRequestWithBase<{ travelPlanId: string; permission: SharePermission }>(
    base,
    '/api/v1/share-links/claim',
    { method: 'POST', body: JSON.stringify({ token }) },
    accessToken,
    null
  );
}
