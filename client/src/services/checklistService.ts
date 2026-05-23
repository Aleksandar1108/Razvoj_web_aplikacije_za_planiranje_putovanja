import { getChecklistApiBaseUrl } from '../config/env';
import { ApiError, apiRequestWithBase } from './httpClient';
import type { ChecklistItem, ChecklistItemCreate } from '../models/checklistItem';

function basePath(planId: string): string {
  return `/api/v1/travel-plans/${encodeURIComponent(planId)}/checklist-items`;
}

function requireBase(): string {
  const b = getChecklistApiBaseUrl();
  if (!b) {
    throw new ApiError('Nije podešen VITE_CHECKLIST_API_BASE_URL u .env fajlu.', 0);
  }
  return b;
}

export async function listChecklistItems(
  planId: string,
  accessToken: string | null,
  shareToken?: string | null
): Promise<ChecklistItem[]> {
  const base = requireBase();
  return apiRequestWithBase<ChecklistItem[]>(base, basePath(planId), { method: 'GET' }, accessToken, shareToken ?? null);
}

export async function createChecklistItem(
  planId: string,
  body: ChecklistItemCreate,
  accessToken: string | null,
  shareToken?: string | null
): Promise<ChecklistItem> {
  const base = requireBase();
  return apiRequestWithBase<ChecklistItem>(
    base,
    basePath(planId),
    { method: 'POST', body: JSON.stringify(body) },
    accessToken,
    shareToken ?? null
  );
}

export async function toggleChecklistItem(
  planId: string,
  itemId: string,
  isDone: boolean,
  accessToken: string | null,
  shareToken?: string | null
): Promise<ChecklistItem> {
  const base = requireBase();
  return apiRequestWithBase<ChecklistItem>(
    base,
    `${basePath(planId)}/${encodeURIComponent(itemId)}/toggle`,
    { method: 'PATCH', body: JSON.stringify({ isDone }) },
    accessToken,
    shareToken ?? null
  );
}

export async function deleteChecklistItem(
  planId: string,
  itemId: string,
  accessToken: string | null,
  shareToken?: string | null
): Promise<void> {
  const base = requireBase();
  await apiRequestWithBase<unknown>(
    base,
    `${basePath(planId)}/${encodeURIComponent(itemId)}`,
    { method: 'DELETE' },
    accessToken,
    shareToken ?? null
  );
}
