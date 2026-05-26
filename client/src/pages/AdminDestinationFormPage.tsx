import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import type { AdminTravelPlanOption } from '../models/adminDestination';
import type { TravelDestinationUpsert } from '../models/travelDestination';
import { adminDestinationsService } from '../services/adminDestinationsService';
import {
  createDestination,
  getDestination,
  updateDestination,
} from '../services/destinationsService';
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

export function AdminDestinationFormPage() {
  const { planId, destinationId } = useParams<{ planId: string; destinationId?: string }>();
  const isEdit = Boolean(destinationId);
  const navigate = useNavigate();
  const { accessToken } = useAuth();

  const [plans, setPlans] = useState<AdminTravelPlanOption[]>([]);
  const [selectedPlanId, setSelectedPlanId] = useState(planId ?? '');
  const [form, setForm] = useState<TravelDestinationUpsert>(emptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const activePlan = plans.find((p) => p.id === selectedPlanId) ?? null;
  const effectivePlanId = isEdit ? planId : selectedPlanId;

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const planList = await adminDestinationsService.listTravelPlans(accessToken);
        if (cancelled) return;
        setPlans(planList);

        const pid = planId ?? planList[0]?.id ?? '';
        setSelectedPlanId(pid);
        const plan = planList.find((p) => p.id === pid) ?? null;

        if (isEdit && planId && destinationId) {
          const d = await getDestination(planId, destinationId, accessToken);
          if (cancelled) return;
          setForm({
            name: d.name,
            location: d.location,
            arrivalDate: d.arrivalDate,
            departureDate: d.departureDate,
            notes: d.notes ?? '',
          });
        } else if (plan) {
          setForm({
            ...emptyForm(),
            arrivalDate: plan.startDate,
            departureDate: plan.endDate,
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

  useEffect(() => {
    if (isEdit || !selectedPlanId) return;
    const plan = plans.find((p) => p.id === selectedPlanId);
    if (!plan) return;
    setForm((f) => ({
      ...f,
      arrivalDate: plan.startDate,
      departureDate: plan.endDate,
    }));
  }, [selectedPlanId, plans, isEdit]);

  async function onSubmit(ev: FormEvent) {
    ev.preventDefault();
    if (!effectivePlanId) {
      setError('Izaberi plan putovanja.');
      return;
    }
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
    if (activePlan && (form.arrivalDate < activePlan.startDate || form.departureDate > activePlan.endDate)) {
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
        await updateDestination(effectivePlanId, destinationId, payload, accessToken);
      } else {
        await createDestination(effectivePlanId, payload, accessToken);
      }
      navigate('/admin/destinacije', { replace: true });
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
        <Link to="/admin/destinacije" className="back-link">
          ← Nazad na admin destinacije
        </Link>
        <h1 className="form-page-title">{isEdit ? 'Izmeni destinaciju (admin)' : 'Nova destinacija (admin)'}</h1>
        <p className="muted form-page-lead">
          Vlasnik plana će dobiti obaveštenje kada sačuvaš izmene na tuđem planu.
        </p>
      </div>

      <form className="card form travel-plan-form glass-panel" onSubmit={onSubmit}>
        {error ? <p className="error">{error}</p> : null}

        {!isEdit ? (
          <fieldset className="form-fieldset">
            <legend>Plan putovanja</legend>
            <label>
              Korisnikov plan
              <select
                required
                value={selectedPlanId}
                onChange={(e) => setSelectedPlanId(e.target.value)}
              >
                <option value="">— izaberi —</option>
                {plans.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name} — {p.ownerDisplayName} ({p.ownerEmail}) [{p.startDate} — {p.endDate}]
                  </option>
                ))}
              </select>
            </label>
          </fieldset>
        ) : activePlan ? (
          <p className="muted">
            Plan: <strong>{activePlan.name}</strong> — vlasnik: {activePlan.ownerDisplayName} ({activePlan.ownerEmail})
          </p>
        ) : null}

        <fieldset className="form-fieldset">
          <legend>Destinacija</legend>
          <label>
            Naziv
            <input
              required
              maxLength={200}
              value={form.name}
              onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
            />
          </label>
          <label>
            Lokacija
            <input
              required
              maxLength={300}
              value={form.location}
              onChange={(e) => setForm((f) => ({ ...f, location: e.target.value }))}
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
            />
          </label>
        </fieldset>

        <div className="form-actions">
          <button type="submit" className="btn btn-glow primary btn-lg" disabled={saving}>
            {saving ? 'Snimam…' : isEdit ? 'Sačuvaj izmene' : 'Dodaj destinaciju'}
          </button>
          <Link to="/admin/destinacije" className="btn ghost btn-lg">
            Otkaži
          </Link>
        </div>
      </form>
    </div>
  );
}
