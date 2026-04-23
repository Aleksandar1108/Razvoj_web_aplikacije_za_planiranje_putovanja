/** Bazni URL API-ja iz Vite .env (mora prefiks VITE_). */
export function getApiBaseUrl(): string {
  const raw = import.meta.env.VITE_API_BASE_URL as string | undefined;
  const trimmed = (raw ?? '').trim().replace(/\/$/, '');
  return trimmed;
}

/** Bazni URL mikroservisa planova putovanja (TravelPlansApi, drugi port od Web1). */
export function getTravelPlansApiBaseUrl(): string {
  const raw = import.meta.env.VITE_TRAVEL_PLANS_API_BASE_URL as string | undefined;
  const trimmed = (raw ?? '').trim().replace(/\/$/, '');
  return trimmed;
}

/** Mikroservis destinacija (DestinationsApi / ServiceManifest — port 8917). */
export function getDestinationsApiBaseUrl(): string {
  const raw = import.meta.env.VITE_DESTINATIONS_API_BASE_URL as string | undefined;
  const trimmed = (raw ?? '').trim().replace(/\/$/, '');
  return trimmed;
}

/** Mikroservis dnevnih aktivnosti (ActivitiesApi / ServiceManifest — port 8919). */
export function getActivitiesApiBaseUrl(): string {
  const raw = import.meta.env.VITE_ACTIVITIES_API_BASE_URL as string | undefined;
  const trimmed = (raw ?? '').trim().replace(/\/$/, '');
  return trimmed;
}

/** Mikroservis troškova i budžeta (ExpensesApi / ServiceManifest — port 8920). */
export function getExpensesApiBaseUrl(): string {
  const raw = import.meta.env.VITE_EXPENSES_API_BASE_URL as string | undefined;
  const trimmed = (raw ?? '').trim().replace(/\/$/, '');
  return trimmed;
}
