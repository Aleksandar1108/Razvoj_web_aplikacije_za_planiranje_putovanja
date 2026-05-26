import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { listTravelPlans } from '../services/travelPlansService';
import type { TravelPlan } from '../models/travelPlan';
import { ApiError } from '../services/httpClient';
import { formatMoneyEur } from '../utils/money';

function formatShortDate(iso: string): string {
  try {
    return new Date(iso + 'T12:00:00').toLocaleDateString('sr-Latn', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
    });
  } catch {
    return iso;
  }
}

function tripLengthDays(start: string, end: string): number {
  const a = new Date(start + 'T12:00:00').getTime();
  const b = new Date(end + 'T12:00:00').getTime();
  if (Number.isNaN(a) || Number.isNaN(b)) return 0;
  return Math.max(0, Math.round((b - a) / (1000 * 60 * 60 * 24)) + 1);
}

export function TravelPlansListPage() {
  const { accessToken } = useAuth();
  const [plans, setPlans] = useState<TravelPlan[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const data = await listTravelPlans(accessToken);
        if (!cancelled) setPlans(data);
      } catch (e) {
        if (!cancelled) {
          setError(e instanceof ApiError ? e.message : 'Neuspjelo učitavanje planova.');
          setPlans(null);
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [accessToken]);

  const count = plans?.length ?? 0;

  return (
    <div className="plans-page">
      <section className="plans-hero">
        <div className="plans-hero-copy">
          <p className="plans-kicker">Tvoji planovi putovanja</p>
          <h1>Planovi putovanja</h1>
          <div className="plans-hero-actions">
            <Link to="/plans/new" className="btn btn-glow primary btn-lg">
              Novi plan
            </Link>
            {!loading && plans ? (
              <span className="plans-hero-badge">{count === 1 ? '1 plan' : `${count} planova`}</span>
            ) : null}
          </div>
        </div>
      </section>

      {loading ? (
        <div className="card plans-loading glass-panel plans-skeleton">Učitavanje planova…</div>
      ) : error ? (
        <div className="card glass-panel">
          <p className="error">{error}</p>
        </div>
      ) : plans && plans.length === 0 ? (
        <div className="card plans-empty glass-panel plans-empty-card">
          <div className="plans-empty-icon" aria-hidden>
            ◎
          </div>
          <h2>Još nemaš planova</h2>
          <p className="muted">Klikni „Novi plan“ i opiši svoje naredno putovanje — uključujući budžet.</p>
          <Link to="/plans/new" className="btn btn-glow primary btn-lg">
            Kreiraj prvi plan
          </Link>
        </div>
      ) : (
        <div className="plan-grid">
          {plans?.map((p) => (
            <Link key={p.id} to={`/plans/${p.id}`} className="plan-card">
              <div className="plan-card-shine" aria-hidden />
              <div className="plan-card-top">
                <h2 className="plan-card-title">{p.name}</h2>
                <span className="plan-chip">{tripLengthDays(p.startDate, p.endDate)} dana</span>
              </div>
              <p className="plan-card-desc">{p.shortDescription}</p>
              <div className="plan-card-meta">
                <span>
                  {formatShortDate(p.startDate)} — {formatShortDate(p.endDate)}
                </span>
                <span className="plan-budget" title="Planirani budžet">
                  {p.plannedBudget > 0 ? `${formatMoneyEur(p.plannedBudget)} €` : 'Budžet —'}
                </span>
              </div>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
