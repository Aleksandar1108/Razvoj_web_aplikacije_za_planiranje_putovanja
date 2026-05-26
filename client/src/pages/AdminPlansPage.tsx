import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import type { AdminTravelPlanRow } from '../models/adminPlan';
import { adminPlansService } from '../services/adminPlansService';
import { ApiError } from '../services/httpClient';

export function AdminPlansPage() {
  const { accessToken } = useAuth();
  const [rows, setRows] = useState<AdminTravelPlanRow[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setRows(await adminPlansService.listPlans(accessToken));
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Neuspjelo učitavanje.');
      setRows(null);
    } finally {
      setLoading(false);
    }
  }, [accessToken]);

  useEffect(() => {
    void load();
  }, [load]);

  return (
    <div className="admin-page">
      <section className="plans-hero">
        <div className="plans-hero-copy">
          <p className="plans-kicker">Administracija</p>
          <h1>Planovi korisnika</h1>
          <div className="plans-hero-actions" style={{ marginTop: '1rem' }}>
            <Link to="/admin/planovi/new" className="btn btn-glow primary btn-lg">
              Novi plan
            </Link>
            <Link to="/admin/korisnici" className="btn ghost">
              Korisnici
            </Link>
          </div>
        </div>
      </section>

      {loading ? (
        <div className="card glass-panel plans-loading">Učitavanje…</div>
      ) : error ? (
        <div className="card glass-panel">
          <p className="error">{error}</p>
          <button type="button" className="btn ghost" onClick={() => void load()}>
            Pokušaj ponovo
          </button>
        </div>
      ) : rows && rows.length === 0 ? (
        <div className="card glass-panel">
          <p className="muted">Nema planova u bazi.</p>
        </div>
      ) : (
        <div className="card glass-panel admin-table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th>Plan</th>
                <th>Vlasnik</th>
                <th>Datumi</th>
                <th>Akcija</th>
              </tr>
            </thead>
            <tbody>
              {rows?.map((r) => (
                <tr key={r.id}>
                  <td>
                    <strong>{r.name}</strong>
                  </td>
                  <td>
                    <div>{r.ownerDisplayName}</div>
                    <div className="muted">{r.ownerEmail}</div>
                  </td>
                  <td className="muted">
                    {r.startDate} — {r.endDate}
                  </td>
                  <td>
                    <Link to={`/plans/${r.id}`} className="btn ghost btn-sm">
                      Upravljaj planom
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
