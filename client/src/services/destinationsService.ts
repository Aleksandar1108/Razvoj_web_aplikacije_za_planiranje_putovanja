import { getDestinationsApiBaseUrl } from '../config/env';
import { ApiError, apiRequestWithBase } from './httpClient';
import type { TravelDestination, TravelDestinationUpsert } from '../models/travelDestination';

function basePath(planId: string): string {
  return `/api/v1/travel-plans/${encodeURIComponent(planId)}/destinations`;
}

function requireBase(): string {
  const b = getDestinationsApiBaseUrl();
  if (!b) {
    throw new ApiError('Nije podešen VITE_DESTINATIONS_API_BASE_URL u .env fajlu.', 0);
  }
  return b;
}

export async function listDestinations(planId: string, accessToken: string | null): Promise<TravelDestination[]> {
  const base = requireBase();
  return apiRequestWithBase<TravelDestination[]>(base, basePath(planId), { method: 'GET' }, accessToken);
}

export async function getDestination(
  planId: string,
  destinationId: string,
  accessToken: string | null
): Promise<TravelDestination> {
  const base = requireBase();
  return apiRequestWithBase<TravelDestination>(
    base,
    `${basePath(planId)}/${encodeURIComponent(destinationId)}`,
    { method: 'GET' },
    accessToken
  );
}

export async function createDestination(
  planId: string,
  body: TravelDestinationUpsert,
  accessToken: string | null
): Promise<TravelDestination> {
  const base = requireBase();
  return apiRequestWithBase<TravelDestination>(
    base,
    basePath(planId),
    { method: 'POST', body: JSON.stringify(body) },
    accessToken
  );
}

export async function updateDestination(
  planId: string,
  destinationId: string,
  body: TravelDestinationUpsert,
  accessToken: string | null
): Promise<TravelDestination> {
  const base = requireBase();
  return apiRequestWithBase<TravelDestination>(
    base,
    `${basePath(planId)}/${encodeURIComponent(destinationId)}`,
    { method: 'PUT', body: JSON.stringify(body) },
    accessToken
  );
}

export async function deleteDestination(
  planId: string,
  destinationId: string,
  accessToken: string | null
): Promise<void> {
  const base = requireBase();
  await apiRequestWithBase<unknown>(
    base,
    `${basePath(planId)}/${encodeURIComponent(destinationId)}`,
    { method: 'DELETE' },
    accessToken
  );
}
