import { getExpensesApiBaseUrl } from '../config/env';
import { ApiError, apiRequestWithBase } from './httpClient';
import type { ExpenseSummary, TravelExpense, TravelExpenseUpsert } from '../models/travelExpense';

function basePath(planId: string): string {
  return `/api/v1/travel-plans/${encodeURIComponent(planId)}/expenses`;
}

function requireBase(): string {
  const b = getExpensesApiBaseUrl();
  if (!b) {
    throw new ApiError('Nije podešen VITE_API_BASE_URL u .env fajlu (ApiGateway).', 0);
  }
  return b;
}

export async function listExpenses(
  planId: string,
  accessToken: string | null,
  shareToken?: string | null
): Promise<TravelExpense[]> {
  const base = requireBase();
  return apiRequestWithBase<TravelExpense[]>(base, basePath(planId), { method: 'GET' }, accessToken, shareToken ?? null);
}

export async function getExpense(
  planId: string,
  expenseId: string,
  accessToken: string | null,
  shareToken?: string | null
): Promise<TravelExpense> {
  const base = requireBase();
  return apiRequestWithBase<TravelExpense>(
    base,
    `${basePath(planId)}/${encodeURIComponent(expenseId)}`,
    { method: 'GET' },
    accessToken,
    shareToken ?? null
  );
}

export async function getExpenseSummary(
  planId: string,
  accessToken: string | null,
  shareToken?: string | null
): Promise<ExpenseSummary> {
  const base = requireBase();
  return apiRequestWithBase<ExpenseSummary>(
    base,
    `${basePath(planId)}/summary`,
    { method: 'GET' },
    accessToken,
    shareToken ?? null
  );
}

export async function createExpense(
  planId: string,
  body: TravelExpenseUpsert,
  accessToken: string | null,
  shareToken?: string | null
): Promise<TravelExpense> {
  const base = requireBase();
  return apiRequestWithBase<TravelExpense>(
    base,
    basePath(planId),
    { method: 'POST', body: JSON.stringify(body) },
    accessToken,
    shareToken ?? null
  );
}

export async function updateExpense(
  planId: string,
  expenseId: string,
  body: TravelExpenseUpsert,
  accessToken: string | null,
  shareToken?: string | null
): Promise<TravelExpense> {
  const base = requireBase();
  return apiRequestWithBase<TravelExpense>(
    base,
    `${basePath(planId)}/${encodeURIComponent(expenseId)}`,
    { method: 'PUT', body: JSON.stringify(body) },
    accessToken,
    shareToken ?? null
  );
}

export async function deleteExpense(
  planId: string,
  expenseId: string,
  accessToken: string | null,
  shareToken?: string | null
): Promise<void> {
  const base = requireBase();
  await apiRequestWithBase<unknown>(
    base,
    `${basePath(planId)}/${encodeURIComponent(expenseId)}`,
    { method: 'DELETE' },
    accessToken,
    shareToken ?? null
  );
}
