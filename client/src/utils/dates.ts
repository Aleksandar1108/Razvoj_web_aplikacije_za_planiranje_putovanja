/** Lokalni kalendarski datum u YYYY-MM-DD (bez UTC pomaka). */
export function todayIsoDate(): string {
  return toIsoDateFromLocal(new Date());
}

export function toIsoDateFromLocal(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

export function addDaysIso(iso: string, days: number): string {
  const [y, m, d] = iso.split('-').map(Number);
  const date = new Date(y, m - 1, d);
  date.setDate(date.getDate() + days);
  return toIsoDateFromLocal(date);
}

/** Za &lt;input type="date"&gt; — odbaci prazno / pogrešno (npr. 0001-01-01 iz API-ja). */
export function toInputDateValue(value: string | null | undefined, fallback: string): string {
  if (!value?.trim()) return fallback;

  const raw = value.trim().slice(0, 10);
  if (!/^\d{4}-\d{2}-\d{2}$/.test(raw)) return fallback;

  const year = Number(raw.slice(0, 4));
  const month = Number(raw.slice(5, 7));
  const day = Number(raw.slice(8, 10));
  if (year < 2000 || year > 2100 || month < 1 || month > 12 || day < 1 || day > 31) {
    return fallback;
  }

  return raw;
}

export function defaultNewTripDates(): { startDate: string; endDate: string } {
  const startDate = todayIsoDate();
  return { startDate, endDate: addDaysIso(startDate, 7) };
}
