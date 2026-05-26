export function getApiBaseUrl(): string {
  const raw = import.meta.env.VITE_API_BASE_URL as string | undefined;
  const trimmed = (raw ?? '').trim().replace(/\/$/, '');
  return trimmed;
}

export function getAuthApiBaseUrl(): string {
  return getApiBaseUrl();
}

export function getTravelPlansApiBaseUrl(): string {
  const raw = import.meta.env.VITE_TRAVEL_PLANS_API_BASE_URL as string | undefined;
  const trimmed = (raw ?? '').trim().replace(/\/$/, '');
  return trimmed;
}

export function getDestinationsApiBaseUrl(): string {
  const raw = import.meta.env.VITE_DESTINATIONS_API_BASE_URL as string | undefined;
  const trimmed = (raw ?? '').trim().replace(/\/$/, '');
  return trimmed;
}

export function getActivitiesApiBaseUrl(): string {
  const raw = import.meta.env.VITE_ACTIVITIES_API_BASE_URL as string | undefined;
  const trimmed = (raw ?? '').trim().replace(/\/$/, '');
  return trimmed;
}

export function getExpensesApiBaseUrl(): string {
  const raw = import.meta.env.VITE_EXPENSES_API_BASE_URL as string | undefined;
  const trimmed = (raw ?? '').trim().replace(/\/$/, '');
  return trimmed;
}

export function getChecklistApiBaseUrl(): string {
  const raw = import.meta.env.VITE_CHECKLIST_API_BASE_URL as string | undefined;
  const trimmed = (raw ?? '').trim().replace(/\/$/, '');
  return trimmed;
}

export function getSharingApiBaseUrl(): string {
  const raw = import.meta.env.VITE_SHARING_API_BASE_URL as string | undefined;
  const trimmed = (raw ?? '').trim().replace(/\/$/, '');
  return trimmed;
}
