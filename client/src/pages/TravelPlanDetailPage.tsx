import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { deleteTravelPlan, getTravelPlan } from '../services/travelPlansService';
import type { TravelPlan } from '../models/travelPlan';
import { ApiError } from '../services/httpClient';

function formatDate(iso: string): string {
  try {
    return new Date(iso + 'T12:00:00').toLocaleDateString('sr-Latn', {
      weekday: 'long',
      day: 'numeric',
      month: 'long',
      year: 'numeric',
    });
  } catch {
    return iso;
  }
}

function formatBudget(value: number): string {
  return new Intl.NumberFormat('sr-Latn', { maximumFractionDigits: 2, minimumFractionDigits: 0 }).format(value);
}

export function TravelPlanDetailPage() {
  const { planId } = useParams<{ planId: string }>();
  const navigate = useNavigate();
  const { accessToken } = useAuth();
  const [plan, setPlan] = useState<TravelPlan | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [deleting, setDeleting] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);

  useEffect(() => {
    if (!planId) return;
    let cancelled = false;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const p = await getTravelPlan(planId, accessToken);
        if (!cancelled) setPlan(p);
      } catch (e) {
        if (!cancelled) {
          setError(e instanceof ApiError ? e.message : 'Plan nije pronađen.');
          setPlan(null);
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [planId, accessToken]);

  async function onDelete() {
    if (!planId) return;
    setDeleting(true);
    setError(null);
    try {
      await deleteTravelPlan(planId, accessToken);
      navigate('/plans', { replace: true });
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Brisanje nije uspjelo.');
    } finally {
      setDeleting(false);
      setConfirmDelete(false);
    }
  }

  if (loading) {
    return (
      <div className="page plan-detail-page">
        <div className="card">Učitavanje…</div>
      </div>
    );
  }

  if (error && !plan) {
    return (
      <div className="page plan-detail-page">
        <Link to="/plans" className="back-link">
          ← Svi planovi
        </Link>
        <div className="card">
          <p className="error">{error}</p>
        </div>
      </div>
    );
  }

  if (!plan) return null;

  return (
    <div className="page plan-detail-page">
      <Link to="/plans" className="back-link">
        ← Svi planovi
      </Link>

      <header className="plan-detail-header">
        <div>
          <p className="plans-kicker">Detalj plana</p>
          <h1>{plan.name}</h1>
          <p className="plan-detail-sub">{plan.shortDescription}</p>
        </div>
        <div className="plan-detail-toolbar">
          <Link to={`/plans/${plan.id}/edit`} className="btn primary">
            Izmeni
          </Link>
          <button type="button" className="btn danger ghost" onClick={() => setConfirmDelete(true)}>
            Obriši
          </button>
        </div>
      </header>

      {error ? <p className="error">{error}</p> : null}

      {confirmDelete ? (
        <div className="card delete-confirm" role="dialog" aria-modal="true" aria-labelledby="del-title">
          <h2 id="del-title">Obrisati plan?</h2>
          <p className="muted">Ova radnja se ne može poništiti.</p>
          <div className="form-actions">
            <button type="button" className="btn danger" disabled={deleting} onClick={onDelete}>
              {deleting ? 'Brišem…' : 'Da, obriši'}
            </button>
            <button type="button" className="btn ghost" disabled={deleting} onClick={() => setConfirmDelete(false)}>
              Otkaži
            </button>
          </div>
        </div>
      ) : null}

      <div className="plan-detail-layout">
        <section className="card plan-detail-panel">
          <h2>Datumi</h2>
          <dl className="detail-dl">
            <div>
              <dt>Početak</dt>
              <dd>{formatDate(plan.startDate)}</dd>
            </div>
            <div>
              <dt>Kraj</dt>
              <dd>{formatDate(plan.endDate)}</dd>
            </div>
          </dl>
        </section>

        <section className="card plan-detail-panel accent">
          <h2>Budžet</h2>
          <p className="plan-detail-budget">{formatBudget(plan.plannedBudget)}</p>
          <p className="muted small">Iznos je informativan; kasnije možeš vezati troškove.</p>
        </section>

        <section className="card plan-detail-panel wide">
          <h2>Napomene</h2>
          {plan.generalNotes?.trim() ? (
            <p className="plan-notes">{plan.generalNotes}</p>
          ) : (
            <p className="muted">Nema unesenih napomena.</p>
          )}
        </section>

        <section className="card plan-detail-panel meta">
          <p className="muted small">
            Kreirano: {new Date(plan.createdAtUtc).toLocaleString('sr-Latn')} · Zadnja izmena:{' '}
            {new Date(plan.updatedAtUtc).toLocaleString('sr-Latn')}
          </p>
        </section>
      </div>
    </div>
  );
}
