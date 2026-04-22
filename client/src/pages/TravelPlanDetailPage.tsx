import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { deleteTravelPlan, getTravelPlan } from '../services/travelPlansService';
import { deleteDestination, listDestinations } from '../services/destinationsService';
import type { TravelPlan } from '../models/travelPlan';
import type { TravelDestination } from '../models/travelDestination';
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

export function TravelPlanDetailPage() {
  const { planId } = useParams<{ planId: string }>();
  const navigate = useNavigate();
  const { accessToken } = useAuth();
  const [plan, setPlan] = useState<TravelPlan | null>(null);
  const [destinations, setDestinations] = useState<TravelDestination[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [destError, setDestError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [deleting, setDeleting] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [deletingDestId, setDeletingDestId] = useState<string | null>(null);

  useEffect(() => {
    if (!planId) return;
    let cancelled = false;
    (async () => {
      setLoading(true);
      setError(null);
      setDestError(null);
      try {
        const p = await getTravelPlan(planId, accessToken);
        if (cancelled) return;
        setPlan(p);
        try {
          const d = await listDestinations(planId, accessToken);
          if (!cancelled) setDestinations(d);
        } catch (de) {
          if (!cancelled) {
            setDestinations([]);
            setDestError(de instanceof ApiError ? de.message : 'Destinacije nisu učitane.');
          }
        }
      } catch (e) {
        if (!cancelled) {
          setError(e instanceof ApiError ? e.message : 'Plan nije pronađen.');
          setPlan(null);
          setDestinations([]);
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

  async function onDeleteDestination(destinationId: string) {
    if (!planId) return;
    if (!window.confirm('Obrisati ovu destinaciju?')) return;
    setDeletingDestId(destinationId);
    setDestError(null);
    try {
      await deleteDestination(planId, destinationId, accessToken);
      setDestinations((prev) => prev.filter((x) => x.id !== destinationId));
    } catch (e) {
      setDestError(e instanceof ApiError ? e.message : 'Brisanje destinacije nije uspjelo.');
    } finally {
      setDeletingDestId(null);
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

        <section className="card plan-detail-panel wide destination-panel">
          <div className="destination-panel-head">
            <h2>Destinacije</h2>
            <Link to={`/plans/${plan.id}/destinations/new`} className="btn btn-glow primary btn-sm">
              Nova destinacija
            </Link>
          </div>
          {destError ? <p className="error small">{destError}</p> : null}
          {destinations.length === 0 ? (
            <p className="muted">Još nema destinacija za ovaj plan.</p>
          ) : (
            <ul className="destination-list">
              {destinations.map((d) => (
                <li key={d.id} className="destination-item glass-panel">
                  <div className="destination-item-main">
                    <p className="destination-name">{d.name}</p>
                    <p className="destination-location muted small">{d.location}</p>
                    <p className="destination-dates small">
                      {formatShortDate(d.arrivalDate)} — {formatShortDate(d.departureDate)}
                    </p>
                    {d.notes?.trim() ? <p className="destination-notes small">{d.notes}</p> : null}
                  </div>
                  <div className="destination-item-actions">
                    <Link to={`/plans/${plan.id}/destinations/${d.id}/edit`} className="btn ghost btn-sm">
                      Izmeni
                    </Link>
                    <button
                      type="button"
                      className="btn danger ghost btn-sm"
                      disabled={deletingDestId === d.id}
                      onClick={() => onDeleteDestination(d.id)}
                    >
                      {deletingDestId === d.id ? 'Brišem…' : 'Obriši'}
                    </button>
                  </div>
                </li>
              ))}
            </ul>
          )}
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
