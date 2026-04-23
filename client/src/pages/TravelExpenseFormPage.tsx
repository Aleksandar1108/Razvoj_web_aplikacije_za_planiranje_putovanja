import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { getTravelPlan } from '../services/travelPlansService';
import { createExpense, getExpense, updateExpense } from '../services/expensesService';
import type { TravelPlan } from '../models/travelPlan';
import { EXPENSE_CATEGORIES, type TravelExpenseCategory, type TravelExpenseUpsert } from '../models/travelExpense';
import { ApiError } from '../services/httpClient';

function emptyForm(): TravelExpenseUpsert {
  return {
    name: '',
    category: 'other',
    amount: 0,
    expenseDate: '',
    description: '',
  };
}

export function TravelExpenseFormPage() {
  const { planId, expenseId } = useParams<{ planId: string; expenseId?: string }>();
  const isEdit = Boolean(expenseId);
  const navigate = useNavigate();
  const { accessToken } = useAuth();

  const [plan, setPlan] = useState<TravelPlan | null>(null);
  const [form, setForm] = useState<TravelExpenseUpsert>(emptyForm);
  const [amountInput, setAmountInput] = useState('0');
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
        if (isEdit && expenseId) {
          const e = await getExpense(planId, expenseId, accessToken);
          if (cancelled) return;
          setForm({
            name: e.name,
            category: e.category,
            amount: e.amount,
            expenseDate: e.expenseDate,
            description: e.description ?? '',
          });
          setAmountInput(String(e.amount));
        } else {
          setForm((prev) => ({ ...prev, expenseDate: p.startDate }));
          setAmountInput('0');
        }
      } catch (e) {
        if (!cancelled) setError(e instanceof ApiError ? e.message : 'Učitavanje nije uspelo.');
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [planId, expenseId, isEdit, accessToken]);

  async function onSubmit(ev: FormEvent) {
    ev.preventDefault();
    if (!planId) return;
    setSaving(true);
    setError(null);
    const payload: TravelExpenseUpsert = {
      ...form,
      amount: Number.isFinite(form.amount) ? form.amount : 0,
      description: form.description?.trim() ? form.description.trim() : null,
    };
    try {
      if (isEdit && expenseId) {
        await updateExpense(planId, expenseId, payload, accessToken);
      } else {
        await createExpense(planId, payload, accessToken);
      }
      navigate(`/plans/${planId}`, { replace: true });
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Snimanje nije uspelo.');
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
        <h1 className="form-page-title">{isEdit ? 'Izmeni trošak' : 'Novi trošak'}</h1>
        {plan ? (
          <p className="muted form-page-lead">
            Plan: <strong>{plan.name}</strong>. Pratiš koliko je potrošeno i koliko budžeta je preostalo.
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
            <legend>Trošak</legend>
            <label>
              Naziv troška
              <input
                required
                maxLength={200}
                value={form.name}
                onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                placeholder="npr. Hotelski smeštaj"
              />
            </label>
            <label>
              Kategorija
              <select
                value={form.category}
                onChange={(e) => setForm((f) => ({ ...f, category: e.target.value as TravelExpenseCategory }))}
              >
                {EXPENSE_CATEGORIES.map((category) => (
                  <option key={category} value={category}>
                    {category}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Datum
              <input
                type="date"
                required
                value={form.expenseDate}
                onChange={(e) => setForm((f) => ({ ...f, expenseDate: e.target.value }))}
              />
            </label>
            <label>
              Iznos
              <div className="input-currency-wrap">
                <input
                  type="text"
                  inputMode="decimal"
                  className="input-currency"
                  value={amountInput}
                  onChange={(e) => {
                    const raw = e.target.value.replace(',', '.');
                    if (!/^\d*\.?\d{0,2}$/.test(raw)) return;
                    setAmountInput(raw);
                    setForm((f) => ({ ...f, amount: raw === '' ? 0 : Number(raw) }));
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
                placeholder="Dodatne informacije o trošku"
              />
            </label>
          </fieldset>

          <div className="form-actions">
            <button type="submit" className="btn btn-glow primary btn-lg" disabled={saving}>
              {saving ? 'Snimam…' : isEdit ? 'Sačuvaj izmene' : 'Dodaj trošak'}
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
