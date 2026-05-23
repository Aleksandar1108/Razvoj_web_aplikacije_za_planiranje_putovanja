import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { getTravelPlan } from '../services/travelPlansService';
import { createDestination, getDestination, updateDestination } from '../services/destinationsService';
import type { TravelDestinationUpsert } from '../models/travelDestination';
import type { TravelPlan } from '../models/travelPlan';
import { ApiError } from '../services/httpClient';

function emptyForm(): TravelDestinationUpsert {
  return {
    name: '',
    location: '',
    arrivalDate: '',
    departureDate: '',
    notes: '',
  };
}

export function TravelDestinationFormPage() {
  const { planId, destinationId } = useParams<{ planId: string; destinationId?: string }>();
  const isEdit = Boolean(destinationId);
  const navigate = useNavigate();
  const { accessToken } = useAuth();

  const [plan, setPlan] = useState<TravelPlan | null>(null);
  const [form, setForm] = useState<TravelDestinationUpsert>(emptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!planId) return;
    let cancelled = false;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const p = await getTravelPlan(planId, accessToken);
        if (cancelled) return;
        setPlan(p);
        if (isEdit && destinationId) {
          const d = await getDestination(planId, destinationId, accessToken);
          if (cancelled) return;
          setForm({
            name: d.name,
            location: d.location,
            arrivalDate: d.arrivalDate,
            departureDate: d.departureDate,
            notes: d.notes ?? '',
          });
        } else {
          setForm({
            ...emptyForm(),
            arrivalDate: p.startDate,
            departureDate: p.endDate,
          });
        }
      } catch (e) {
        if (!cancelled) setError(e instanceof ApiError ? e.message : 'Učitavanje nije uspjelo.');
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [planId, destinationId, isEdit, accessToken]);

  async function onSubmit(ev: FormEvent) {
    ev.preventDefault();
    if (!planId) return;
    setSaving(true);
    setError(null);

    if (!form.arrivalDate || !form.departureDate) {
      setError('Izaberi datume dolaska i odlaska.');
      setSaving(false);
      return;
    }
    if (form.departureDate < form.arrivalDate) {
      setError('Datum odlaska ne može biti pre datuma dolaska.');
      setSaving(false);
      return;
    }
    if (plan && (form.arrivalDate < plan.startDate || form.departureDate > plan.endDate)) {
      setError('Datumi destinacije moraju biti u okviru datuma plana putovanja.');
      setSaving(false);
      return;
    }

    const payload: TravelDestinationUpsert = {
      ...form,
      notes: form.notes?.trim() ? form.notes.trim() : null,
    };
    try {
      if (isEdit && destinationId) {
        await updateDestination(planId, destinationId, payload, accessToken);
        navigate(`/plans/${planId}`, { replace: true });
      } else {
        await createDestination(planId, payload, accessToken);
        navigate(`/plans/${planId}`, { replace: true });
      }
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Snimanje nije uspjelo.');
    } finally {
      setSaving(false);
    }
  }

  if (!planId) {
    return null;
  }

  if (loading) {
    return (
      <div className="page plans-form-page">
        <div className="card glass-panel plans-form-loading">Učitavanje…</div>
      </div>
    );
  }

  return (
    <div className="page plans-form-page">
      <div className="form-page-head">
        <Link to={`/plans/${planId}`} className="back-link">
          ← Nazad na plan
        </Link>
        <h1 className="form-page-title">{isEdit ? 'Izmeni destinaciju' : 'Nova destinacija'}</h1>
        {plan ? (
          <p className="muted form-page-lead">
            Plan: <strong>{plan.name}</strong> ({plan.startDate} — {plan.endDate}). Datumi destinacije moraju biti u tom opsegu.
          </p>
        ) : (
          <p className="muted form-page-lead">Nije moguće učitati plan.</p>
        )}
      </div>

      {error && !plan ? (
        <div className="card glass-panel">
          <p className="error">{error}</p>
        </div>
      ) : null}

      {plan ? (
        <form className="card form travel-plan-form glass-panel" onSubmit={onSubmit}>
          {error ? <p className="error">{error}</p> : null}

          <fieldset className="form-fieldset">
            <legend>Destinacija</legend>
            <label>
              Naziv
              <input
                required
                maxLength={200}
                value={form.name}
                onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                placeholder="npr. Tenerife"
              />
            </label>
            <label>
              Lokacija
              <input
                required
                maxLength={300}
                value={form.location}
                onChange={(e) => setForm((f) => ({ ...f, location: e.target.value }))}
                placeholder="Grad, regija ili adresa"
              />
            </label>
          </fieldset>

          <fieldset className="form-fieldset">
            <legend>Datumi</legend>
            <div className="field-grid-2">
              <label>
                Datum dolaska
                <input
                  type="date"
                  required
                  value={form.arrivalDate}
                  onChange={(e) => setForm((f) => ({ ...f, arrivalDate: e.target.value }))}
                />
              </label>
              <label>
                Datum odlaska
                <input
                  type="date"
                  required
                  value={form.departureDate}
                  onChange={(e) => setForm((f) => ({ ...f, departureDate: e.target.value }))}
                />
              </label>
            </div>
          </fieldset>

          <fieldset className="form-fieldset">
            <legend>Napomena</legend>
            <label>
              Kratak opis / napomena
              <textarea
                maxLength={1000}
                rows={3}
                value={form.notes ?? ''}
                onChange={(e) => setForm((f) => ({ ...f, notes: e.target.value }))}
                placeholder="Šta želiš da zapamtiš za ovo mjesto?"
              />
            </label>
          </fieldset>

          <div className="form-actions">
            <button type="submit" className="btn btn-glow primary btn-lg" disabled={saving}>
              {saving ? 'Snimam…' : isEdit ? 'Sačuvaj izmene' : 'Dodaj destinaciju'}
            </button>
            <Link to={`/plans/${planId}`} className="btn ghost btn-lg">
              Otkaži
            </Link>
          </div>
        </form>
      ) : null}
    </div>
  );
}
