import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { createTravelPlan, getTravelPlan, updateTravelPlan } from '../services/travelPlansService';
import type { TravelPlanUpsert } from '../models/travelPlan';
import { ApiError } from '../services/httpClient';

function emptyForm(): TravelPlanUpsert {
  return {
    name: '',
    shortDescription: '',
    startDate: '',
    endDate: '',
    plannedBudget: 0,
    generalNotes: '',
  };
}

export function TravelPlanFormPage() {
  const { planId } = useParams<{ planId: string }>();
  const isEdit = Boolean(planId);
  const navigate = useNavigate();
  const { accessToken } = useAuth();

  const [form, setForm] = useState<TravelPlanUpsert>(emptyForm);
  const [loading, setLoading] = useState(isEdit);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!isEdit || !planId) return;
    let cancelled = false;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const p = await getTravelPlan(planId, accessToken);
        if (cancelled) return;
        setForm({
          name: p.name,
          shortDescription: p.shortDescription,
          startDate: p.startDate,
          endDate: p.endDate,
          plannedBudget: p.plannedBudget,
          generalNotes: p.generalNotes ?? '',
        });
      } catch (e) {
        if (!cancelled) setError(e instanceof ApiError ? e.message : 'Plan nije pronađen.');
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [isEdit, planId, accessToken]);

  async function onSubmit(ev: FormEvent) {
    ev.preventDefault();
    setSaving(true);
    setError(null);
    const payload: TravelPlanUpsert = {
      ...form,
      generalNotes: form.generalNotes?.trim() ? form.generalNotes.trim() : null,
    };
    try {
      if (isEdit && planId) {
        await updateTravelPlan(planId, payload, accessToken);
        navigate(`/plans/${planId}`, { replace: true });
      } else {
        const created = await createTravelPlan(payload, accessToken);
        navigate(`/plans/${created.id}`, { replace: true });
      }
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Snimanje nije uspjelo.');
    } finally {
      setSaving(false);
    }
  }

  if (loading) {
    return (
      <div className="page plans-form-page">
        <div className="card">Učitavanje…</div>
      </div>
    );
  }

  return (
    <div className="page plans-form-page">
      <div className="form-page-head">
        <Link to={isEdit && planId ? `/plans/${planId}` : '/plans'} className="back-link">
          ← Nazad
        </Link>
        <h1>{isEdit ? 'Izmijeni plan' : 'Novi plan putovanja'}</h1>
        <p className="muted">
          {isEdit
            ? 'Ažuriraj podatke; datumi i budžet moraju ostati smisleni.'
            : 'Unesi osnovne podatke — kasnije možeš dodati destinacije i stavke iz specifikacije.'}
        </p>
      </div>

      <form className="card form travel-plan-form" onSubmit={onSubmit}>
        {error ? <p className="error">{error}</p> : null}

        <label>
          Naziv putovanja
          <input
            required
            maxLength={200}
            value={form.name}
            onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
            placeholder="npr. Proljeće u Barceloni"
          />
        </label>

        <label>
          Kratak opis
          <textarea
            required
            maxLength={500}
            rows={3}
            value={form.shortDescription}
            onChange={(e) => setForm((f) => ({ ...f, shortDescription: e.target.value }))}
            placeholder="Šta te vuče na ovo putovanje?"
          />
        </label>

        <div className="field-grid-2">
          <label>
            Početni datum
            <input
              type="date"
              required
              value={form.startDate}
              onChange={(e) => setForm((f) => ({ ...f, startDate: e.target.value }))}
            />
          </label>
          <label>
            Krajnji datum
            <input
              type="date"
              required
              value={form.endDate}
              onChange={(e) => setForm((f) => ({ ...f, endDate: e.target.value }))}
            />
          </label>
        </div>

        <label>
          Planirani budžet
          <input
            type="number"
            min={0}
            step="0.01"
            required
            value={Number.isNaN(form.plannedBudget) ? '' : form.plannedBudget}
            onChange={(e) => setForm((f) => ({ ...f, plannedBudget: Number(e.target.value) }))}
          />
        </label>

        <label>
          Opšte napomene
          <textarea
            maxLength={4000}
            rows={4}
            value={form.generalNotes ?? ''}
            onChange={(e) => setForm((f) => ({ ...f, generalNotes: e.target.value }))}
            placeholder="Vize, parking, deca, zdravlje…"
          />
        </label>

        <div className="form-actions">
          <button type="submit" className="btn primary" disabled={saving}>
            {saving ? 'Snimam…' : isEdit ? 'Sačuvaj izmjene' : 'Kreiraj plan'}
          </button>
          <Link to={isEdit && planId ? `/plans/${planId}` : '/plans'} className="btn ghost">
            Otkaži
          </Link>
        </div>
      </form>
    </div>
  );
}
