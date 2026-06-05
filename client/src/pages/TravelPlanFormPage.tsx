import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { createTravelPlan, getTravelPlan, updateTravelPlan } from '../services/travelPlansService';
import type { TravelPlanUpsert } from '../models/travelPlan';
import { ApiError } from '../services/httpClient';
import { addDaysIso, defaultNewTripDates, todayIsoDate, toInputDateValue } from '../utils/dates';
import { roundMoney } from '../utils/money';

function emptyForm(): TravelPlanUpsert {
  const { startDate, endDate } = defaultNewTripDates();
  return {
    name: '',
    shortDescription: '',
    startDate,
    endDate,
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
        const startDate = toInputDateValue(p.startDate, todayIsoDate());
        const endDate = toInputDateValue(p.endDate, addDaysIso(startDate, 7));
        setForm({
          name: p.name,
          shortDescription: p.shortDescription,
          startDate,
          endDate: endDate < startDate ? addDaysIso(startDate, 7) : endDate,
          plannedBudget: roundMoney(p.plannedBudget),
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

  const budgetInputValue =
    isEdit || form.plannedBudget !== 0 ? (Number.isNaN(form.plannedBudget) ? '' : String(form.plannedBudget)) : '';
  const minStartDate = isEdit ? undefined : todayIsoDate();
  const minEndDate = form.startDate || todayIsoDate();

  async function onSubmit(ev: FormEvent) {
    ev.preventDefault();
    setSaving(true);
    setError(null);

    if (!form.startDate || !form.endDate) {
      setError('Izaberi početni i krajnji datum.');
      setSaving(false);
      return;
    }
    if (form.endDate < form.startDate) {
      setError('Krajnji datum ne može biti prije početnog.');
      setSaving(false);
      return;
    }
    if (form.plannedBudget < 0) {
      setError('Planirani budžet ne može biti negativan.');
      setSaving(false);
      return;
    }

    const payload: TravelPlanUpsert = {
      ...form,
      plannedBudget: roundMoney(form.plannedBudget),
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
        <div className="card glass-panel plans-form-loading">Učitavanje…</div>
      </div>
    );
  }

  return (
    <div className="page plans-form-page">
      <div className="form-page-head">
        <Link to={isEdit && planId ? `/plans/${planId}` : '/plans'} className="back-link">
          ← Nazad
        </Link>
        <h1 className="form-page-title">{isEdit ? 'Izmeni plan' : 'Novi plan putovanja'}</h1>
        <p className="muted form-page-lead">
          {isEdit
            ? 'Ažuriraj podatke; datumi i budžet moraju ostati smisleni.'
            : 'Popuni naziv, opis, datume i budžet — ostalo je opciono.'}
        </p>
      </div>

      <form className="card form travel-plan-form glass-panel" onSubmit={onSubmit}>
        {error ? <p className="error">{error}</p> : null}

        <fieldset className="form-fieldset">
          <legend>Osnova</legend>
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
        </fieldset>

        <fieldset className="form-fieldset">
          <legend>Datumi</legend>
          <div className="field-grid-2">
            <label>
              Početni datum
              <input
                type="date"
                required
                min={minStartDate}
                value={form.startDate}
                onChange={(e) => {
                  const startDate = e.target.value;
                  setForm((f) => {
                    const endDate =
                      f.endDate && f.endDate >= startDate ? f.endDate : addDaysIso(startDate, 7);
                    return { ...f, startDate, endDate };
                  });
                }}
              />
            </label>
            <label>
              Krajnji datum
              <input
                type="date"
                required
                min={minEndDate}
                value={form.endDate}
                onChange={(e) => setForm((f) => ({ ...f, endDate: e.target.value }))}
              />
            </label>
          </div>
        </fieldset>

        <fieldset className="form-fieldset form-fieldset-budget">
          <legend>Finansije</legend>
          <label className="label-budget">
            <span className="label-budget-text">Planirani budžet</span>
            <span className="input-currency-wrap">
              <input
                type="number"
                inputMode="decimal"
                min={0}
                step={1}
                className="input-currency"
                placeholder={isEdit ? undefined : 'npr. 1500'}
                value={budgetInputValue}
                onChange={(e) => {
                  const raw = e.target.value;
                  const n = raw === '' ? 0 : Number(raw);
                  setForm((f) => ({ ...f, plannedBudget: Number.isNaN(n) ? 0 : n }));
                }}
              />
              <span className="input-currency-suffix" aria-hidden>
                EUR
              </span>
            </span>
            <span className="field-hint">Ukupan okvir koji planiraš za ovo putovanje (možeš kasnije prilagoditi).</span>
          </label>
        </fieldset>

        <fieldset className="form-fieldset">
          <legend>Napomene</legend>
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
        </fieldset>

        <div className="form-actions">
          <button type="submit" className="btn btn-glow primary btn-lg" disabled={saving}>
            {saving ? 'Snimam…' : isEdit ? 'Sačuvaj izmene' : 'Kreiraj plan'}
          </button>
          <Link to={isEdit && planId ? `/plans/${planId}` : '/plans'} className="btn ghost btn-lg">
            Otkaži
          </Link>
        </div>
      </form>
    </div>
  );
}
