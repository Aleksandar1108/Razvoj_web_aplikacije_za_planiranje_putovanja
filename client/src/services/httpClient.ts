import { getApiBaseUrl } from '../config/env';

export async function apiRequestWithBase<T>(
  baseUrl: string,
  path: string,
  init: RequestInit = {},
  accessToken?: string | null
): Promise<T> {
  const base = baseUrl.trim().replace(/\/$/, '');
  if (!base) {
    throw new ApiError('API bazni URL je prazan.', 0);
  }

  const headers = new Headers(init.headers);
  if (!headers.has('Content-Type') && init.body) {
    headers.set('Content-Type', 'application/json');
  }
  if (accessToken) {
    headers.set('Authorization', `Bearer ${accessToken}`);
  }

  const res = await fetch(`${base}${path}`, { ...init, headers });
  const text = await res.text();
  let data: unknown;
  if (!text) {
    data = undefined;
  } else {
    try {
      data = JSON.parse(text);
    } catch {
      data = text;
    }
  }

  if (!res.ok) {
    throw new ApiError(extractMessage(res.status, data), res.status, data);
  }

  return data as T;
}

export class ApiError extends Error {
  readonly status: number;
  readonly body: unknown;

  constructor(message: string, status: number, body?: unknown) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.body = body;
  }
}

function extractMessage(status: number, data: unknown): string {
  if (data && typeof data === 'object') {
    const o = data as Record<string, unknown>;
    if (typeof o.message === 'string') return o.message;
    if (typeof o.title === 'string') return o.title;
    if (o.errors && typeof o.errors === 'object') {
      const errs = o.errors as Record<string, string[]>;
      const first = Object.values(errs).flat()[0];
      if (first) return first;
    }
  }
  return `Zahtjev nije uspio (${status}).`;
}

export async function apiRequest<T>(
  path: string,
  init: RequestInit = {},
  accessToken?: string | null
): Promise<T> {
  const base = getApiBaseUrl();
  if (!base) {
    throw new ApiError('Nije podešen VITE_API_BASE_URL u .env fajlu.', 0);
  }
  return apiRequestWithBase<T>(base, path, init, accessToken);
}
