import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { listTravelPlans } from '../services/travelPlansService';
import type { TravelPlan } from '../models/travelPlan';
import { ApiError } from '../services/httpClient';

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

function formatBudget(value: number): string {
  return new Intl.NumberFormat('sr-Latn', { maximumFractionDigits: 2, minimumFractionDigits: 0 }).format(value);
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

  return (
    <div className="plans-page">
      <section className="plans-hero">
        <div className="plans-hero-copy">
          <p className="plans-kicker">Mikroservis TravelPlansApi</p>
          <h1>Planovi putovanja</h1>
          <p className="plans-lead">
            Kreiraj putovanje sa nazivom, opisom, datumima, budžetom i napomenama. Lista i detalji su vezani za tvoj nalog.
          </p>
          <div className="plans-hero-actions">
            <Link to="/plans/new" className="btn primary">
              Novi plan
            </Link>
          </div>
        </div>
        <div className="plans-hero-art" aria-hidden />
      </section>

      {loading ? (
        <div className="card plans-loading">Učitavanje planova…</div>
      ) : error ? (
        <div className="card">
          <p className="error">{error}</p>
        </div>
      ) : plans && plans.length === 0 ? (
        <div className="card plans-empty">
          <h2>Još nemaš planova</h2>
          <p className="muted">Klikni „Novi plan“ i opiši svoje naredno putovanje.</p>
          <Link to="/plans/new" className="btn primary">
            Kreiraj prvi plan
          </Link>
        </div>
      ) : (
        <div className="plan-grid">
          {plans?.map((p) => (
            <Link key={p.id} to={`/plans/${p.id}`} className="plan-card">
              <div className="plan-card-top">
                <h2 className="plan-card-title">{p.name}</h2>
                <span className="plan-chip">{tripLengthDays(p.startDate, p.endDate)} dana</span>
              </div>
              <p className="plan-card-desc">{p.shortDescription}</p>
              <div className="plan-card-meta">
                <span>
                  {formatShortDate(p.startDate)} — {formatShortDate(p.endDate)}
                </span>
                <span className="plan-budget">{formatBudget(p.plannedBudget)}</span>
              </div>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
