/** Za prikaz i unos budžeta/troškova — uvek celi EUR, bez ,99 artefakata. */
export function roundMoney(value: number): number {
  if (!Number.isFinite(value)) return 0;
  return Math.round(value);
}

export function formatMoneyEur(value: number): string {
  return new Intl.NumberFormat('sr-Latn', {
    maximumFractionDigits: 0,
    minimumFractionDigits: 0,
  }).format(roundMoney(value));
}
