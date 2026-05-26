import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { listSharedPlans } from '../services/sharingService';
import type { SharedTravelPlanListItem } from '../models/share';
import { ApiError } from '../services/httpClient';

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

function permissionLabel(p: string): string {
  if (p === 'edit') return 'Uređivanje';
  if (p === 'view') return 'Pregled';
  return p;
}

export function SharedPlansPage() {
  const { accessToken } = useAuth();
  const [rows, setRows] = useState<SharedTravelPlanListItem[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const data = await listSharedPlans(accessToken);
        if (!cancelled) setRows(data);
      } catch (e) {
        if (!cancelled) {
          setRows(null);
          setError(e instanceof ApiError ? e.message : 'Neuspjelo učitavanje deljenih planova.');
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [accessToken]);

  const count = rows?.length ?? 0;
  const sorted = useMemo(() => rows ?? [], [rows]);

  return (
    <div className="plans-page">
      <section className="plans-hero">
        <div className="plans-hero-copy">
          <p className="plans-kicker">Pristup preko QR / linka</p>
          <h1>Deljeni planovi</h1>
          <div className="plans-hero-actions">
            <Link to="/share/qr" className="btn btn-glow primary btn-lg">
              Učitaj QR
            </Link>
            {!loading && rows ? (
              <span className="plans-hero-badge">{count === 1 ? '1 plan' : `${count} planova`}</span>
            ) : null}
          </div>
        </div>
      </section>

      {loading ? (
        <div className="card plans-loading glass-panel plans-skeleton">Učitavanje…</div>
      ) : error ? (
        <div className="card glass-panel">
          <p className="error">{error}</p>
        </div>
      ) : sorted.length === 0 ? (
        <div className="card plans-empty glass-panel plans-empty-card">
          <div className="plans-empty-icon" aria-hidden>
            ⧉
          </div>
          <h2>Još nema deljenih planova</h2>
          <p className="muted">Učitaj QR kod da povežeš plan sa svojim nalogom.</p>
          <Link to="/share/qr" className="btn btn-glow primary btn-lg">
            Učitaj QR
          </Link>
        </div>
      ) : (
        <div className="card glass-panel" style={{ overflow: 'auto' }}>
          <table className="table">
            <thead>
              <tr>
                <th>Plan</th>
                <th>Datumi</th>
                <th>Dozvola</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {sorted.map((r) => (
                <tr key={`${r.travelPlanId}:${r.updatedAtUtc}`}>
                  <td>
                    <div style={{ fontWeight: 650 }}>{r.name}</div>
                    <div className="muted" style={{ marginTop: 6 }}>
                      {r.shortDescription}
                    </div>
                  </td>
                  <td className="muted">
                    {formatShortDate(r.startDate)} — {formatShortDate(r.endDate)}
                  </td>
                  <td>
                    <span className="pill">{permissionLabel(r.permission)}</span>
                  </td>
                  <td style={{ textAlign: 'right' }}>
                    <Link className="btn ghost" to={`/plans/${r.travelPlanId}?p=${encodeURIComponent(r.permission)}`}>
                      Otvori
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
