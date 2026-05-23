import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { getTravelPlan } from '../services/travelPlansService';
import { createActivity, getActivity, updateActivity } from '../services/activitiesService';
import { ACTIVITY_STATUSES, type TravelActivityStatus, type TravelActivityUpsert } from '../models/travelActivity';
import type { TravelPlan } from '../models/travelPlan';
import { ApiError } from '../services/httpClient';

function emptyForm(): TravelActivityUpsert {
  return {
    name: '',
    activityDate: '',
    activityTime: '09:00',
    location: '',
    description: '',
    estimatedCost: 0,
    status: 'planned',
  };
}

export function TravelActivityFormPage() {
  const { planId, activityId } = useParams<{ planId: string; activityId?: string }>();
  const isEdit = Boolean(activityId);
  const navigate = useNavigate();
  const { accessToken } = useAuth();

  const [plan, setPlan] = useState<TravelPlan | null>(null);
  const [form, setForm] = useState<TravelActivityUpsert>(emptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [estimatedCostInput, setEstimatedCostInput] = useState('0');

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
        if (isEdit && activityId) {
          const a = await getActivity(planId, activityId, accessToken);
          if (cancelled) return;
          setForm({
            name: a.name,
            activityDate: a.activityDate,
            activityTime: a.activityTime.slice(0, 5),
            location: a.location,
            description: a.description ?? '',
            estimatedCost: a.estimatedCost,
            status: a.status,
          });
          setEstimatedCostInput(String(a.estimatedCost));
        } else {
          setForm((prev) => ({ ...prev, activityDate: p.startDate }));
          setEstimatedCostInput('0');
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
  }, [planId, activityId, isEdit, accessToken]);

  async function onSubmit(ev: FormEvent) {
    ev.preventDefault();
    if (!planId) return;
    setSaving(true);
    setError(null);

    const estimatedCost = Number.isFinite(form.estimatedCost) ? form.estimatedCost : 0;
    if (estimatedCost < 0) {
      setError('Procijenjeni trošak ne može biti negativan.');
      setSaving(false);
      return;
    }
    if (!form.activityDate) {
      setError('Izaberi datum aktivnosti.');
      setSaving(false);
      return;
    }
    if (plan && (form.activityDate < plan.startDate || form.activityDate > plan.endDate)) {
      setError('Datum aktivnosti mora biti u okviru datuma plana putovanja.');
      setSaving(false);
      return;
    }

    const payload: TravelActivityUpsert = {
      ...form,
      description: form.description?.trim() ? form.description.trim() : null,
      status: form.status,
      estimatedCost,
    };
    try {
      if (isEdit && activityId) {
        await updateActivity(planId, activityId, payload, accessToken);
      } else {
        await createActivity(planId, payload, accessToken);
      }
      navigate(`/plans/${planId}`, { replace: true });
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Snimanje nije uspjelo.');
    } finally {
      setSaving(false);
    }
  }

  if (!planId) return null;

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
        <h1 className="form-page-title">{isEdit ? 'Izmeni aktivnost' : 'Nova aktivnost'}</h1>
        {plan ? (
          <p className="muted form-page-lead">
            Plan: <strong>{plan.name}</strong> ({plan.startDate} — {plan.endDate}). Datum aktivnosti mora biti u tom opsegu.
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
            <legend>Aktivnost</legend>
            <label>
              Naziv
              <input
                required
                maxLength={200}
                value={form.name}
                onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                placeholder="npr. Muzej savremene umjetnosti"
              />
            </label>
            <label>
              Lokacija
              <input
                required
                maxLength={300}
                value={form.location}
                onChange={(e) => setForm((f) => ({ ...f, location: e.target.value }))}
                placeholder="Adresa ili naziv lokacije"
              />
            </label>
          </fieldset>

          <fieldset className="form-fieldset">
            <legend>Vreme i status</legend>
            <div className="field-grid-2">
              <label>
                Datum
                <input
                  type="date"
                  required
                  value={form.activityDate}
                  onChange={(e) => setForm((f) => ({ ...f, activityDate: e.target.value }))}
                />
              </label>
              <label>
                Vreme
                <input
                  type="time"
                  required
                  value={form.activityTime}
                  onChange={(e) => setForm((f) => ({ ...f, activityTime: e.target.value }))}
                />
              </label>
            </div>
            <label>
              Status
              <select
                value={form.status}
                onChange={(e) => setForm((f) => ({ ...f, status: e.target.value as TravelActivityStatus }))}
              >
                {ACTIVITY_STATUSES.map((status) => (
                  <option key={status} value={status}>
                    {status}
                  </option>
                ))}
              </select>
            </label>
          </fieldset>

          <fieldset className="form-fieldset">
            <legend>Trošak i opis</legend>
            <label>
              Procenjeni trošak
              <div className="input-currency-wrap">
                <input
                  type="text"
                  inputMode="decimal"
                  className="input-currency"
                  value={estimatedCostInput}
                  onChange={(e) => {
                    const raw = e.target.value.replace(',', '.');
                    if (!/^\d*\.?\d{0,2}$/.test(raw)) return;
                    setEstimatedCostInput(raw);
                    setForm((f) => ({ ...f, estimatedCost: raw === '' ? 0 : Number(raw) }));
                  }}
                />
                <span className="input-currency-suffix">EUR</span>
              </div>
            </label>
            <label>
              Opis
              <textarea
                maxLength={2000}
                rows={3}
                value={form.description ?? ''}
                onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
                placeholder="Dodatni detalji aktivnosti"
              />
            </label>
          </fieldset>

          <div className="form-actions">
            <button type="submit" className="btn btn-glow primary btn-lg" disabled={saving}>
              {saving ? 'Snimam…' : isEdit ? 'Sačuvaj izmjene' : 'Dodaj aktivnost'}
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
