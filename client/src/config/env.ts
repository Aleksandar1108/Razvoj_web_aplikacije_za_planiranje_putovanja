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
