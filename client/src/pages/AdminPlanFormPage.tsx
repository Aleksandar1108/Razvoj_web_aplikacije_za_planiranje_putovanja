import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import type { AdminUserRow } from '../models/admin';
import type { TravelPlanUpsert } from '../models/travelPlan';
import { adminService } from '../services/adminService';
import { adminPlansService } from '../services/adminPlansService';
import { ApiError } from '../services/httpClient';
import { roundMoney } from '../utils/money';

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

function userLabel(u: AdminUserRow): string {
  const name = `${u.firstName} ${u.lastName}`.trim() || 'Korisnik';
  return `${name} (${u.email})`;
}

export function AdminPlanFormPage() {
  const navigate = useNavigate();
  const { accessToken } = useAuth();

  const [users, setUsers] = useState<AdminUserRow[]>([]);
  const [ownerUserId, setOwnerUserId] = useState('');
  const [form, setForm] = useState<TravelPlanUpsert>(emptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const list = await adminService.listUsers(accessToken);
        if (cancelled) return;
        setUsers(list);
        if (list.length > 0) setOwnerUserId(list[0].id);
      } catch (e) {
        if (!cancelled) setError(e instanceof ApiError ? e.message : 'Neuspjelo učitavanje korisnika.');
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [accessToken]);

  const budgetInputValue =
    form.plannedBudget !== 0 ? (Number.isNaN(form.plannedBudget) ? '' : String(form.plannedBudget)) : '';

  async function onSubmit(ev: FormEvent) {
    ev.preventDefault();
    setSaving(true);
    setError(null);

    if (!ownerUserId) {
      setError('Izaberi korisnika.');
      setSaving(false);
      return;
    }
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

    const payload = {
      ownerUserId,
      ...form,
      plannedBudget: roundMoney(form.plannedBudget),
      generalNotes: form.generalNotes?.trim() ? form.generalNotes.trim() : null,
    };

    try {
      const created = await adminPlansService.createPlan(accessToken, payload);
      navigate(`/plans/${created.id}`, { replace: true });
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Snimanje nije uspjelo.');
    } finally {
      setSaving(false);
    }
  }

  if (loading) {
    return (
      <div className="page plans-form-page admin-page">
        <div className="card glass-panel plans-form-loading">Učitavanje…</div>
      </div>
    );
  }

  return (
    <div className="page plans-form-page admin-page">
      <div className="form-page-head">
        <Link to="/admin/planovi" className="back-link">
          ← Nazad na planove
        </Link>
        <h1 className="form-page-title">Novi plan za korisnika</h1>
      </div>

      {users.length === 0 ? (
        <div className="card glass-panel">
          <p className="muted">Nema korisnika u sistemu.</p>
          <Link to="/admin/korisnici" className="btn ghost">
            Korisnici
          </Link>
        </div>
      ) : (
        <form className="card form travel-plan-form glass-panel" onSubmit={onSubmit}>
          {error ? <p className="error">{error}</p> : null}

          <fieldset className="form-fieldset">
            <legend>Vlasnik plana</legend>
            <label>
              Korisnik
              <select
                required
                value={ownerUserId}
                onChange={(e) => setOwnerUserId(e.target.value)}
              >
                {users.map((u) => (
                  <option key={u.id} value={u.id}>
                    {userLabel(u)}
                    {!u.isActive ? ' — neaktivan' : ''}
                  </option>
                ))}
              </select>
            </label>
          </fieldset>

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
                  placeholder="npr. 1500"
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
              {saving ? 'Snimam…' : 'Kreiraj plan'}
            </button>
            <Link to="/admin/planovi" className="btn ghost btn-lg">
              Otkaži
            </Link>
          </div>
        </form>
      )}
    </div>
  );
}
