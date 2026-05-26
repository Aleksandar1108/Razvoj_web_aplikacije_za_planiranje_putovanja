import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import QRCode from 'qrcode';
import { useAuth } from '../context/AuthContext';
import { deleteTravelPlan, getTravelPlan } from '../services/travelPlansService';
import { deleteDestination, listDestinations } from '../services/destinationsService';
import { deleteActivity, listActivities } from '../services/activitiesService';
import { deleteExpense, getExpenseSummary, listExpenses } from '../services/expensesService';
import { createChecklistItem, deleteChecklistItem, listChecklistItems, toggleChecklistItem } from '../services/checklistService';
import { createShareLink } from '../services/sharingService';
import type { TravelPlan } from '../models/travelPlan';
import type { TravelDestination } from '../models/travelDestination';
import type { TravelActivity } from '../models/travelActivity';
import type { ExpenseSummary, TravelExpense } from '../models/travelExpense';
import type { ChecklistItem } from '../models/checklistItem';
import type { SharePermission } from '../models/share';
import { ApiError } from '../services/httpClient';
import { formatMoneyEur, roundMoney } from '../utils/money';

type PlanSection = 'osnovno' | 'destinacije' | 'troskovi' | 'aktivnosti' | 'kalendar' | 'checklista' | 'napomene';

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

function formatTime(value: string): string {
  return value?.slice(0, 5) ?? value;
}

function formatStatus(status: string): string {
  if (status === 'planned') return 'Planirano';
  if (status === 'reserved') return 'Rezervisano';
  if (status === 'completed') return 'Završeno';
  if (status === 'cancelled') return 'Otkazano';
  return status;
}

function statusClass(status: string): string {
  return `activity-status activity-status-${status}`;
}

function categoryLabel(category: string): string {
  if (category === 'transport') return 'Prevoz';
  if (category === 'accommodation') return 'Smeštaj';
  if (category === 'food') return 'Hrana';
  if (category === 'tickets') return 'Ulaznice';
  if (category === 'shopping') return 'Kupovina';
  if (category === 'other') return 'Ostalo';
  return category;
}

function buildSummary(plannedBudget: number, expenses: TravelExpense[], activities: TravelActivity[]): ExpenseSummary {
  const totalExpenseEntries = roundMoney(expenses.reduce((sum, item) => sum + item.amount, 0));
  const totalActivityEstimatedCosts = roundMoney(
    activities.reduce((sum, item) => sum + item.estimatedCost, 0)
  );
  const totalExpenses = roundMoney(totalExpenseEntries + totalActivityEstimatedCosts);
  const planned = roundMoney(plannedBudget);
  return {
    plannedBudget: planned,
    totalExpenses,
    totalExpenseEntries,
    totalActivityEstimatedCosts,
    remainingBudget: roundMoney(planned - totalExpenses),
  };
}

function parseDateOnly(iso: string): Date {
  return new Date(`${iso}T12:00:00`);
}

function toIsoDate(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

function monthStart(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), 1);
}

function addDays(date: Date, days: number): Date {
  const d = new Date(date);
  d.setDate(d.getDate() + days);
  return d;
}

export function TravelPlanDetailPage() {
  const { planId } = useParams<{ planId: string }>();
  const navigate = useNavigate();
  const { accessToken, user } = useAuth();
  const isAdmin = user?.role === 'Admin';
  const [searchParams] = useSearchParams();
  const shareHeaderToken = useMemo(() => {
    const raw = searchParams.get('t');
    const trimmed = (raw ?? '').trim();
    return trimmed.length > 0 ? trimmed : null;
  }, [searchParams]);

  const sharePermQuery = useMemo(() => {
    const raw = (searchParams.get('p') ?? '').trim().toLowerCase();
    if (raw === 'view' || raw === 'edit') return raw as SharePermission;
    return null;
  }, [searchParams]);

  const [plan, setPlan] = useState<TravelPlan | null>(null);
  const [destinations, setDestinations] = useState<TravelDestination[]>([]);
  const [activities, setActivities] = useState<TravelActivity[]>([]);
  const [expenses, setExpenses] = useState<TravelExpense[]>([]);
  const [checklistItems, setChecklistItems] = useState<ChecklistItem[]>([]);
  const [expenseSummary, setExpenseSummary] = useState<ExpenseSummary | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [destError, setDestError] = useState<string | null>(null);
  const [actError, setActError] = useState<string | null>(null);
  const [expError, setExpError] = useState<string | null>(null);
  const [checklistError, setChecklistError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [deleting, setDeleting] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [deletingDestId, setDeletingDestId] = useState<string | null>(null);
  const [deletingActId, setDeletingActId] = useState<string | null>(null);
  const [deletingExpId, setDeletingExpId] = useState<string | null>(null);
  const [deletingChecklistId, setDeletingChecklistId] = useState<string | null>(null);
  const [selectedCalendarDate, setSelectedCalendarDate] = useState<string | null>(null);
  const [activeSection, setActiveSection] = useState<PlanSection>('osnovno');
  const [newChecklistTitle, setNewChecklistTitle] = useState('');
  const [addingChecklist, setAddingChecklist] = useState(false);

  const [sharePermissionPick, setSharePermissionPick] = useState<SharePermission>('view');
  const [shareBusy, setShareBusy] = useState(false);
  const [shareError, setShareError] = useState<string | null>(null);
  const [shareQrPngDataUrl, setShareQrPngDataUrl] = useState<string | null>(null);

  useEffect(() => {
    if (!planId) return;
    let cancelled = false;
    (async () => {
      setLoading(true);
      setError(null);
      setDestError(null);
      setActError(null);
      setExpError(null);
      setChecklistError(null);
      try {
        const p = await getTravelPlan(planId, accessToken, shareHeaderToken);
        if (cancelled) return;
        setPlan({ ...p, plannedBudget: roundMoney(p.plannedBudget) });
        try {
          const d = await listDestinations(planId, accessToken, shareHeaderToken);
          if (!cancelled) setDestinations(d);
        } catch (de) {
          if (!cancelled) {
            setDestinations([]);
            setDestError(de instanceof ApiError ? de.message : 'Destinacije nisu učitane.');
          }
        }
        try {
          const a = await listActivities(planId, accessToken, shareHeaderToken);
          if (!cancelled) setActivities(a);
        } catch (ae) {
          if (!cancelled) {
            setActivities([]);
            setActError(ae instanceof ApiError ? ae.message : 'Aktivnosti nisu učitane.');
          }
        }
        try {
          const e = await listExpenses(planId, accessToken, shareHeaderToken);
          if (!cancelled) setExpenses(e);
          const s = await getExpenseSummary(planId, accessToken, shareHeaderToken);
          if (!cancelled) {
            setExpenseSummary({
              ...s,
              plannedBudget: roundMoney(s.plannedBudget),
              totalExpenses: roundMoney(s.totalExpenses),
              totalExpenseEntries: roundMoney(s.totalExpenseEntries),
              totalActivityEstimatedCosts: roundMoney(s.totalActivityEstimatedCosts),
              remainingBudget: roundMoney(s.remainingBudget),
            });
          }
        } catch (ee) {
          if (!cancelled) {
            setExpenses([]);
            setExpenseSummary(null);
            setExpError(ee instanceof ApiError ? ee.message : 'Troškovi nisu učitani.');
          }
        }
        try {
          const items = await listChecklistItems(planId, accessToken, shareHeaderToken);
          if (!cancelled) setChecklistItems(items);
        } catch (ce) {
          if (!cancelled) {
            setChecklistItems([]);
            setChecklistError(ce instanceof ApiError ? ce.message : 'Checklist nije učitan.');
          }
        }
      } catch (e) {
        if (!cancelled) {
          setError(e instanceof ApiError ? e.message : 'Plan nije pronađen.');
          setPlan(null);
          setDestinations([]);
          setActivities([]);
          setExpenses([]);
          setChecklistItems([]);
          setExpenseSummary(null);
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [planId, accessToken, shareHeaderToken]);

  async function onDelete() {
    if (!planId) return;
    setDeleting(true);
    setError(null);
    try {
      await deleteTravelPlan(planId, accessToken, shareHeaderToken);
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
      await deleteDestination(planId, destinationId, accessToken, shareHeaderToken);
      setDestinations((prev) => prev.filter((x) => x.id !== destinationId));
    } catch (e) {
      setDestError(e instanceof ApiError ? e.message : 'Brisanje destinacije nije uspjelo.');
    } finally {
      setDeletingDestId(null);
    }
  }

  async function onDeleteActivity(activityId: string) {
    if (!planId) return;
    if (!window.confirm('Obrisati ovu aktivnost?')) return;
    setDeletingActId(activityId);
    setActError(null);
    try {
      await deleteActivity(planId, activityId, accessToken, shareHeaderToken);
      const nextActivities = activities.filter((x) => x.id !== activityId);
      setActivities(nextActivities);
      const plannedBudget = plan?.plannedBudget ?? 0;
      setExpenseSummary(buildSummary(plannedBudget, expenses, nextActivities));
    } catch (e) {
      setActError(e instanceof ApiError ? e.message : 'Brisanje aktivnosti nije uspjelo.');
    } finally {
      setDeletingActId(null);
    }
  }

  async function onDeleteExpense(expenseId: string) {
    if (!planId) return;
    if (!window.confirm('Obrisati ovaj trošak?')) return;
    setDeletingExpId(expenseId);
    setExpError(null);
    try {
      await deleteExpense(planId, expenseId, accessToken, shareHeaderToken);
      const next = expenses.filter((x) => x.id !== expenseId);
      setExpenses(next);
      const plannedBudget = plan?.plannedBudget ?? 0;
      setExpenseSummary(buildSummary(plannedBudget, next, activities));
    } catch (e) {
      setExpError(e instanceof ApiError ? e.message : 'Brisanje troška nije uspelo.');
    } finally {
      setDeletingExpId(null);
    }
  }

  async function onAddChecklistItem() {
    if (!planId) return;
    const title = newChecklistTitle.trim();
    if (!title) return;
    setAddingChecklist(true);
    setChecklistError(null);
    try {
      const created = await createChecklistItem(planId, { title }, accessToken, shareHeaderToken);
      setChecklistItems((prev) => [...prev, created].sort((a, b) => Number(a.isDone) - Number(b.isDone)));
      setNewChecklistTitle('');
    } catch (e) {
      setChecklistError(e instanceof ApiError ? e.message : 'Dodavanje stavke nije uspelo.');
    } finally {
      setAddingChecklist(false);
    }
  }

  async function onToggleChecklistItem(item: ChecklistItem, isDone: boolean) {
    if (!planId) return;
    setChecklistError(null);
    try {
      const updated = await toggleChecklistItem(planId, item.id, isDone, accessToken, shareHeaderToken);
      setChecklistItems((prev) =>
        prev
          .map((x) => (x.id === item.id ? updated : x))
          .sort((a, b) => Number(a.isDone) - Number(b.isDone))
      );
    } catch (e) {
      setChecklistError(e instanceof ApiError ? e.message : 'Promena statusa nije uspela.');
    }
  }

  async function onDeleteChecklistItem(itemId: string) {
    if (!planId) return;
    setDeletingChecklistId(itemId);
    setChecklistError(null);
    try {
      await deleteChecklistItem(planId, itemId, accessToken, shareHeaderToken);
      setChecklistItems((prev) => prev.filter((x) => x.id !== itemId));
    } catch (e) {
      setChecklistError(e instanceof ApiError ? e.message : 'Brisanje stavke nije uspelo.');
    } finally {
      setDeletingChecklistId(null);
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

  const budgetSummary =
    expenseSummary ?? buildSummary(roundMoney(plan.plannedBudget), expenses, activities);

  const activitiesByDate = activities.reduce<Record<string, TravelActivity[]>>((acc, item) => {
    if (!acc[item.activityDate]) acc[item.activityDate] = [];
    acc[item.activityDate].push(item);
    return acc;
  }, {});

  const groupedDates = Object.keys(activitiesByDate).sort((a, b) => a.localeCompare(b));
  const selectedDayActivities = selectedCalendarDate ? (activitiesByDate[selectedCalendarDate] ?? []) : [];
  const planStart = parseDateOnly(plan.startDate);
  const planEnd = parseDateOnly(plan.endDate);
  const calendarMonths: Date[] = [];
  {
    const cursor = monthStart(planStart);
    const last = monthStart(planEnd);
    while (cursor <= last) {
      calendarMonths.push(new Date(cursor));
      cursor.setMonth(cursor.getMonth() + 1);
    }
  }

  const canMutateUi = sharePermQuery === null || sharePermQuery === 'edit';
  const showOwnerToolbar = sharePermQuery === null;

  async function onGenerateShareQr() {
    if (!planId) return;
    setShareBusy(true);
    setShareError(null);
    setShareQrPngDataUrl(null);
    try {
      const created = await createShareLink(planId, sharePermissionPick, accessToken);
      const dataUrl = await QRCode.toDataURL(created.qrPayloadJson, {
        errorCorrectionLevel: 'M',
        margin: 2,
        width: 512,
      });
      setShareQrPngDataUrl(dataUrl);
    } catch (e) {
      setShareError(e instanceof ApiError ? e.message : 'Generisanje QR koda nije uspjelo.');
    } finally {
      setShareBusy(false);
    }
  }

  function onDownloadShareQrPng() {
    if (!shareQrPngDataUrl || !planId) return;
    const a = document.createElement('a');
    a.href = shareQrPngDataUrl;
    a.download = `plan-${planId}-share-qr.png`;
    a.click();
  }

  return (
    <div className="page plan-detail-page">
      <Link to={isAdmin ? '/admin/planovi' : '/plans'} className="back-link">
        ← {isAdmin ? 'Admin planovi' : 'Svi planovi'}
      </Link>

      {isAdmin ? (
        <div className="card glass-panel admin-plan-banner">
          <p className="muted" style={{ margin: 0 }}>
            Pregledate plan kao <strong>administrator</strong>. Svaka izmena u bilo kojoj sekciji menija šalje vlasniku
            detaljno obaveštenje.
          </p>
        </div>
      ) : null}

      <header className="plan-detail-header">
        <div>
          <p className="plans-kicker">Detalj plana</p>
          <h1>{plan.name}</h1>
          <p className="plan-detail-sub">{plan.shortDescription}</p>
        </div>
        <div className="plan-detail-toolbar">
          {showOwnerToolbar ? (
            <Link to={`/plans/${plan.id}/edit`} className="btn primary">
              Izmeni
            </Link>
          ) : (
            <span className="pill">Deljen plan · {sharePermQuery === 'edit' ? 'uređivanje' : 'pregled'}</span>
          )}
          {showOwnerToolbar ? (
            <button type="button" className="btn danger ghost" onClick={() => setConfirmDelete(true)}>
              Obriši
            </button>
          ) : null}
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

      <section className="plan-overview-strip">
        <article className="plan-overview-card card">
          <p className="muted small">Period</p>
          <p className="overview-value">
            {formatShortDate(plan.startDate)} - {formatShortDate(plan.endDate)}
          </p>
        </article>
        <article className="plan-overview-card card">
          <p className="muted small">Destinacije</p>
          <p className="overview-value">{destinations.length}</p>
        </article>
        <article className="plan-overview-card card">
          <p className="muted small">Aktivnosti</p>
          <p className="overview-value">{activities.length}</p>
        </article>
        <article className="plan-overview-card card">
          <p className="muted small">Troškovi (stavke)</p>
          <p className="overview-value">{expenses.length}</p>
        </article>
        <article className="plan-overview-card card">
          <p className="muted small">Checklist</p>
          <p className="overview-value">
            {checklistItems.filter((x) => x.isDone).length}/{checklistItems.length}
          </p>
        </article>
      </section>

      <div className="plan-workspace-layout">
        <div className="plan-workspace-content">
          {activeSection === 'osnovno' ? (
            <>
              <section id="sekcija-osnovno" className="card plan-detail-panel">
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
                <div className="budget-osnovno-row">
                  <div>
                    <p className="muted small">Planirani budžet</p>
                    <p className="plan-detail-budget">{formatMoneyEur(budgetSummary.plannedBudget)} EUR</p>
                  </div>
                  <div>
                    <p className="muted small">Preostali budžet</p>
                    <p className="plan-detail-budget plan-detail-budget-remaining">
                      {formatMoneyEur(budgetSummary.remainingBudget)} EUR
                    </p>
                  </div>
                </div>
                <p className="muted small">
                  Preostalo nakon troškova i procenjenih aktivnosti ({formatMoneyEur(budgetSummary.totalExpenses)} EUR
                  potrošeno).
                </p>
              </section>

              {showOwnerToolbar ? (
                <section className="card plan-detail-panel wide destination-panel">
                  <div className="destination-panel-head">
                    <h2>Deljenje (QR)</h2>
                  </div>
                  <p className="muted small">
                    Izaberi nivo pristupa, generiši QR i preuzmi PNG. Drugi korisnik učitava QR u sekciji „Učitaj QR“.
                  </p>

                  <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap', marginTop: 12, alignItems: 'center' }}>
                    <label className="muted small" style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                      <span>Pristup</span>
                      <select
                        className="input"
                        value={sharePermissionPick}
                        onChange={(e) => setSharePermissionPick(e.target.value as SharePermission)}
                      >
                        <option value="view">Pregled (VIEW)</option>
                        <option value="edit">Uređivanje (EDIT)</option>
                      </select>
                    </label>
                    <button type="button" className="btn btn-glow primary btn-sm" disabled={shareBusy} onClick={onGenerateShareQr}>
                      {shareBusy ? 'Generišem…' : 'Generiši QR'}
                    </button>
                    <button type="button" className="btn ghost btn-sm" disabled={!shareQrPngDataUrl} onClick={onDownloadShareQrPng}>
                      Preuzmi PNG
                    </button>
                  </div>

                  {shareError ? (
                    <p className="error small" style={{ marginTop: 10 }}>
                      {shareError}
                    </p>
                  ) : null}

                  {shareQrPngDataUrl ? (
                    <div style={{ marginTop: 12 }}>
                      <img
                        alt="QR kod za deljenje plana"
                        src={shareQrPngDataUrl}
                        style={{ width: 240, height: 240, borderRadius: 16, border: '1px solid rgba(15, 23, 42, 0.12)' }}
                      />
                    </div>
                  ) : null}
                </section>
              ) : null}
            </>
          ) : null}

          {activeSection === 'destinacije' ? (
            <section id="sekcija-destinacije" className="card plan-detail-panel wide destination-panel">
          <div className="destination-panel-head">
            <h2>Destinacije</h2>
            {canMutateUi ? (
              <Link to={`/plans/${plan.id}/destinations/new`} className="btn btn-glow primary btn-sm">
                Nova destinacija
              </Link>
            ) : null}
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
                  {canMutateUi ? (
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
                  ) : null}
                </li>
              ))}
            </ul>
          )}
            </section>
          ) : null}

          {activeSection === 'troskovi' ? (
            <section id="sekcija-troskovi" className="card plan-detail-panel wide destination-panel">
          <div className="destination-panel-head">
            <h2>Troškovi i budžet</h2>
            {canMutateUi ? (
              <Link to={`/plans/${plan.id}/expenses/new`} className="btn btn-glow primary btn-sm">
                Novi trošak
              </Link>
            ) : null}
          </div>
          {expError ? <p className="error small">{expError}</p> : null}
          <div className="expense-summary-grid">
            <div className="expense-summary-box">
              <p className="muted small">Planirani budžet</p>
              <p className="expense-summary-value">{formatMoneyEur(budgetSummary.plannedBudget)} EUR</p>
            </div>
            <div className="expense-summary-box">
              <p className="muted small">Ukupno potrošeno</p>
              <p className="expense-summary-value">{formatMoneyEur(budgetSummary.totalExpenses)} EUR</p>
            </div>
            <div className="expense-summary-box">
              <p className="muted small">Troškovi (stavke)</p>
              <p className="expense-summary-value">{formatMoneyEur(budgetSummary.totalExpenseEntries)} EUR</p>
            </div>
            <div className="expense-summary-box">
              <p className="muted small">Aktivnosti (procenjeno)</p>
              <p className="expense-summary-value">{formatMoneyEur(budgetSummary.totalActivityEstimatedCosts)} EUR</p>
            </div>
            <div className="expense-summary-box">
              <p className="muted small">Preostali budžet</p>
              <p className="expense-summary-value">{formatMoneyEur(budgetSummary.remainingBudget)} EUR</p>
            </div>
          </div>
          {expenses.length === 0 ? (
            <p className="muted">Još nema evidentiranih troškova.</p>
          ) : (
            <ul className="destination-list">
              {expenses.map((e) => (
                <li key={e.id} className="destination-item glass-panel">
                  <div className="destination-item-main">
                    <p className="destination-name">{e.name}</p>
                    <p className="destination-location muted small">
                      {categoryLabel(e.category)} · {formatShortDate(e.expenseDate)}
                    </p>
                    <p className="small">
                      Iznos: <strong>{formatMoneyEur(e.amount)} EUR</strong>
                    </p>
                    <p className="destination-notes small">{e.description ?? 'Bez opisa'}</p>
                  </div>
                  {canMutateUi ? (
                    <div className="destination-item-actions">
                      <Link to={`/plans/${plan.id}/expenses/${e.id}/edit`} className="btn ghost btn-sm">
                        Izmeni
                      </Link>
                      <button
                        type="button"
                        className="btn danger ghost btn-sm"
                        disabled={deletingExpId === e.id}
                        onClick={() => onDeleteExpense(e.id)}
                      >
                        {deletingExpId === e.id ? 'Brišem…' : 'Obriši'}
                      </button>
                    </div>
                  ) : null}
                </li>
              ))}
            </ul>
          )}
            </section>
          ) : null}

          {activeSection === 'aktivnosti' ? (
            <section id="sekcija-aktivnosti" className="card plan-detail-panel wide destination-panel">
          <div className="destination-panel-head">
            <h2>Aktivnosti po danima</h2>
            {canMutateUi ? (
              <Link to={`/plans/${plan.id}/activities/new`} className="btn btn-glow primary btn-sm">
                Nova aktivnost
              </Link>
            ) : null}
          </div>
          {actError ? <p className="error small">{actError}</p> : null}
          {groupedDates.length === 0 ? (
            <p className="muted">Još nema aktivnosti za ovaj plan.</p>
          ) : (
            <div className="activity-day-list">
              {groupedDates.map((day) => (
                <div key={day} className="activity-day-group">
                  <p className="activity-day-title">{formatDate(day)}</p>
                  <ul className="destination-list">
                    {activitiesByDate[day].map((a) => (
                      <li key={a.id} className="destination-item glass-panel">
                        <div className="destination-item-main">
                          <p className="destination-name">
                            {formatTime(a.activityTime)} · {a.name}
                          </p>
                          <p className="destination-location muted small">{a.location}</p>
                          <p className="destination-notes small">{a.description ?? 'Bez opisa'}</p>
                          <p className="small">
                            Procenjeni trošak: <strong>{formatMoneyEur(a.estimatedCost)}</strong> EUR
                            {' · '}
                            <span className={statusClass(a.status)}>{formatStatus(a.status)}</span>
                          </p>
                        </div>
                        {canMutateUi ? (
                          <div className="destination-item-actions">
                            <Link to={`/plans/${plan.id}/activities/${a.id}/edit`} className="btn ghost btn-sm">
                              Izmeni
                            </Link>
                            <button
                              type="button"
                              className="btn danger ghost btn-sm"
                              disabled={deletingActId === a.id}
                              onClick={() => onDeleteActivity(a.id)}
                            >
                              {deletingActId === a.id ? 'Brišem…' : 'Obriši'}
                            </button>
                          </div>
                        ) : null}
                      </li>
                    ))}
                  </ul>
                </div>
              ))}
            </div>
          )}
            </section>
          ) : null}

          {activeSection === 'kalendar' ? (
            <section id="sekcija-kalendar" className="card plan-detail-panel wide">
          <h2>Kalendar aktivnosti</h2>
          {calendarMonths.map((monthDate) => {
            const firstDay = monthStart(monthDate);
            const firstWeekday = (firstDay.getDay() + 6) % 7;
            const gridStart = addDays(firstDay, -firstWeekday);
            const cells = Array.from({ length: 42 }, (_, idx) => {
              const day = addDays(gridStart, idx);
              const iso = toIsoDate(day);
              return {
                iso,
                inMonth: day.getMonth() === monthDate.getMonth(),
                items: activitiesByDate[iso] ?? [],
              };
            });

            return (
              <div key={monthDate.toISOString()} className="calendar-month">
                <p className="calendar-month-title">
                  {monthDate.toLocaleDateString('sr-Latn', { month: 'long', year: 'numeric' })}
                </p>
                <div className="calendar-weekdays">
                  <span>Pon</span>
                  <span>Uto</span>
                  <span>Sre</span>
                  <span>Čet</span>
                  <span>Pet</span>
                  <span>Sub</span>
                  <span>Ned</span>
                </div>
                <div className="calendar-grid">
                  {cells.map((cell) => (
                    <button
                      key={cell.iso}
                      type="button"
                      className={`calendar-cell${cell.inMonth ? '' : ' muted-cell'}${cell.items.length ? ' has-items' : ''}${
                        selectedCalendarDate === cell.iso ? ' selected' : ''
                      }`}
                      onClick={() => setSelectedCalendarDate(cell.iso)}
                    >
                      <p className="calendar-day-number">{Number(cell.iso.slice(8, 10))}</p>
                      {cell.items.length > 0 ? (
                        <ul className="calendar-items">
                          {cell.items.slice(0, 2).map((item) => (
                            <li key={item.id}>
                              {formatTime(item.activityTime)} {item.name}
                            </li>
                          ))}
                          {cell.items.length > 2 ? <li>+{cell.items.length - 2} još</li> : null}
                        </ul>
                      ) : null}
                    </button>
                  ))}
                </div>
              </div>
            );
          })}
          {selectedCalendarDate ? (
            <div className="calendar-popover" role="dialog" aria-modal="false" aria-label="Aktivnosti za izabrani datum">
              <div className="calendar-popover-header">
                <p className="calendar-selected-title">Aktivnosti za {formatDate(selectedCalendarDate)}</p>
                <div className="calendar-popover-actions">
                  {canMutateUi ? (
                    <Link
                      to={`/plans/${plan.id}/activities/new?date=${selectedCalendarDate}`}
                      className="btn btn-glow primary btn-sm"
                    >
                      Nova aktivnost
                    </Link>
                  ) : null}
                  <button type="button" className="btn ghost btn-sm" onClick={() => setSelectedCalendarDate(null)}>
                    Zatvori
                  </button>
                </div>
              </div>
              {selectedDayActivities.length > 0 ? (
                <ul className="calendar-popover-list">
                  {selectedDayActivities
                    .slice()
                    .sort((a, b) => a.activityTime.localeCompare(b.activityTime))
                    .map((a) => (
                      <li key={a.id} className="calendar-popover-item">
                        <p className="destination-name">
                          {formatTime(a.activityTime)} · {a.name}
                        </p>
                        <p className="destination-location muted small">{a.location}</p>
                        <p className="destination-notes small">{a.description ?? 'Bez opisa'}</p>
                        <p className="small">
                          Procenjeni trošak: <strong>{formatMoneyEur(a.estimatedCost)}</strong> EUR
                          {' · '}
                          <span className={statusClass(a.status)}>{formatStatus(a.status)}</span>
                        </p>
                        {canMutateUi ? (
                          <div className="destination-item-actions">
                            <Link to={`/plans/${plan.id}/activities/${a.id}/edit`} className="btn ghost btn-sm">
                              Izmeni
                            </Link>
                            <button
                              type="button"
                              className="btn danger ghost btn-sm"
                              disabled={deletingActId === a.id}
                              onClick={() => void onDeleteActivity(a.id)}
                            >
                              {deletingActId === a.id ? 'Brišem…' : 'Obriši'}
                            </button>
                          </div>
                        ) : null}
                      </li>
                    ))}
                </ul>
              ) : (
                <div className="calendar-popover-empty">
                  <p className="muted small">Nema aktivnosti za izabrani datum.</p>
                  {canMutateUi ? (
                    <Link
                      to={`/plans/${plan.id}/activities/new?date=${selectedCalendarDate}`}
                      className="btn btn-glow primary btn-sm"
                    >
                      Dodaj aktivnost
                    </Link>
                  ) : null}
                </div>
              )}
            </div>
          ) : null}
            </section>
          ) : null}

          {activeSection === 'checklista' ? (
            <section className="card plan-detail-panel wide destination-panel">
              <div className="destination-panel-head">
                <h2>Checklist / packing lista</h2>
              </div>
              {checklistError ? <p className="error small">{checklistError}</p> : null}

              <div className="checklist-add-row">
                <input
                  className="checklist-input"
                  placeholder="Dodaj stavku (npr. pasoš, karta, punjač...)"
                  value={newChecklistTitle}
                  disabled={!canMutateUi}
                  onChange={(e) => setNewChecklistTitle(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') {
                      e.preventDefault();
                      void onAddChecklistItem();
                    }
                  }}
                />
                <button
                  type="button"
                  className="btn primary btn-sm"
                  disabled={!canMutateUi || addingChecklist}
                  onClick={() => void onAddChecklistItem()}
                >
                  {addingChecklist ? 'Dodajem…' : 'Dodaj'}
                </button>
              </div>

              {checklistItems.length === 0 ? (
                <p className="muted">Još nema checklist stavki.</p>
              ) : (
                <ul className="checklist-list">
                  {checklistItems.map((item) => (
                    <li key={item.id} className={`checklist-item${item.isDone ? ' done' : ''}`}>
                      <label className="checklist-main">
                        <input
                          type="checkbox"
                          checked={item.isDone}
                          disabled={!canMutateUi}
                          onChange={(e) => void onToggleChecklistItem(item, e.target.checked)}
                        />
                        <span>{item.title}</span>
                      </label>
                      {canMutateUi ? (
                        <button
                          type="button"
                          className="btn danger ghost btn-sm"
                          disabled={deletingChecklistId === item.id}
                          onClick={() => void onDeleteChecklistItem(item.id)}
                        >
                          {deletingChecklistId === item.id ? 'Brišem…' : 'Obriši'}
                        </button>
                      ) : null}
                    </li>
                  ))}
                </ul>
              )}
            </section>
          ) : null}

          {activeSection === 'napomene' ? (
            <section id="sekcija-napomene" className="card plan-detail-panel wide">
              <h2>Napomene</h2>
              {plan.generalNotes?.trim() ? (
                <p className="plan-notes">{plan.generalNotes}</p>
              ) : (
                <p className="muted">Nema unesenih napomena.</p>
              )}
            </section>
          ) : null}

          <section className="card plan-detail-panel meta">
            <p className="muted small">
              Kreirano: {new Date(plan.createdAtUtc).toLocaleString('sr-Latn')} · Zadnja izmena:{' '}
              {new Date(plan.updatedAtUtc).toLocaleString('sr-Latn')}
            </p>
          </section>
        </div>

        <aside className="plan-workspace-menu card" aria-label="Meni funkcionalnosti plana">
          <p className="workspace-menu-title">Meni plana</p>
          <button type="button" className={`workspace-menu-item${activeSection === 'osnovno' ? ' active' : ''}`} onClick={() => setActiveSection('osnovno')}>
            Osnovno
          </button>
          <button type="button" className={`workspace-menu-item${activeSection === 'destinacije' ? ' active' : ''}`} onClick={() => setActiveSection('destinacije')}>
            Destinacije
          </button>
          <button type="button" className={`workspace-menu-item${activeSection === 'troskovi' ? ' active' : ''}`} onClick={() => setActiveSection('troskovi')}>
            Troškovi i budžet
          </button>
          <button type="button" className={`workspace-menu-item${activeSection === 'aktivnosti' ? ' active' : ''}`} onClick={() => setActiveSection('aktivnosti')}>
            Aktivnosti po danima
          </button>
          <button type="button" className={`workspace-menu-item${activeSection === 'kalendar' ? ' active' : ''}`} onClick={() => setActiveSection('kalendar')}>
            Kalendar aktivnosti
          </button>
          <button type="button" className={`workspace-menu-item${activeSection === 'checklista' ? ' active' : ''}`} onClick={() => setActiveSection('checklista')}>
            Checklist / Packing
          </button>
          <button type="button" className={`workspace-menu-item${activeSection === 'napomene' ? ' active' : ''}`} onClick={() => setActiveSection('napomene')}>
            Napomene
          </button>
        </aside>
      </div>
    </div>
  );
}
