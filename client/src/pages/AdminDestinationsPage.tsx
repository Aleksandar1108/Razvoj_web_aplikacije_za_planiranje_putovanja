import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import type { AdminDestinationRow } from '../models/adminDestination';
import { adminDestinationsService } from '../services/adminDestinationsService';
import { deleteDestination } from '../services/destinationsService';
import { ApiError } from '../services/httpClient';

export function AdminDestinationsPage() {
  const { accessToken } = useAuth();
  const [rows, setRows] = useState<AdminDestinationRow[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setRows(await adminDestinationsService.listDestinations(accessToken));
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

  const onDelete = async (row: AdminDestinationRow) => {
    if (!window.confirm(`Obrisati destinaciju "${row.name}"? Vlasnik plana ce dobiti obavestenje.`)) {
      return;
    }
    setBusyId(row.id);
    setError(null);
    try {
      await deleteDestination(row.travelPlanId, row.id, accessToken);
      setRows((prev) => (prev ? prev.filter((r) => r.id !== row.id) : prev));
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Brisanje nije uspjelo.');
    } finally {
      setBusyId(null);
    }
  };

  return (
    <div className="admin-page">
      <section className="plans-hero">
        <div className="plans-hero-copy">
          <p className="plans-kicker">Administracija</p>
          <h1>Destinacije korisnika</h1>
          <p className="plans-lead">
            Pregled svih destinacija po planovima — vidiš čije je šta. Izmene i brisanja obavještavaju vlasnika plana.
          </p>
          <div className="form-actions" style={{ marginTop: '1rem' }}>
            <Link to="/admin/destinacije/new" className="btn btn-glow primary">
              Dodaj destinaciju
            </Link>
            <Link to="/admin/planovi" className="btn ghost">
              Upravljanje planovima
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
          <p className="muted">Nema destinacija u bazi.</p>
        </div>
      ) : (
        <div className="card glass-panel admin-table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th>Destinacija</th>
                <th>Plan</th>
                <th>Vlasnik</th>
                <th>Datumi</th>
                <th>Akcije</th>
              </tr>
            </thead>
            <tbody>
              {rows?.map((r) => {
                const busy = busyId === r.id;
                return (
                  <tr key={r.id}>
                    <td>
                      <strong>{r.name}</strong>
                      <div className="muted">{r.location}</div>
                    </td>
                    <td>{r.travelPlanName}</td>
                    <td>
                      <div>{r.ownerDisplayName}</div>
                      <div className="muted">{r.ownerEmail}</div>
                    </td>
                    <td className="muted">
                      {r.arrivalDate} — {r.departureDate}
                    </td>
                    <td className="admin-actions">
                      <Link
                        to={`/admin/destinacije/${r.travelPlanId}/${r.id}/edit`}
                        className="btn ghost btn-sm"
                      >
                        Izmeni
                      </Link>
                      <button
                        type="button"
                        className="btn ghost btn-sm danger"
                        disabled={busy}
                        onClick={() => void onDelete(r)}
                      >
                        Obriši
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
