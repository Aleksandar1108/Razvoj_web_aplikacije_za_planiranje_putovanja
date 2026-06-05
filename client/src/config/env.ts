export function getApiBaseUrl(): string {
  const raw = import.meta.env.VITE_API_BASE_URL as string | undefined;
  const trimmed = (raw ?? '').trim().replace(/\/$/, '');
  return trimmed;
}

/** Svi javni API pozivi idu kroz ApiGateway (YARP) — jedan ulaz za React. */
export function getAuthApiBaseUrl(): string {
  return getApiBaseUrl();
}

export function getTravelPlansApiBaseUrl(): string {
  return getApiBaseUrl();
}

export function getDestinationsApiBaseUrl(): string {
  return getApiBaseUrl();
}

export function getActivitiesApiBaseUrl(): string {
  return getApiBaseUrl();
}

export function getExpensesApiBaseUrl(): string {
  return getApiBaseUrl();
}

export function getChecklistApiBaseUrl(): string {
  return getApiBaseUrl();
}

export function getSharingApiBaseUrl(): string {
  return getApiBaseUrl();
}
