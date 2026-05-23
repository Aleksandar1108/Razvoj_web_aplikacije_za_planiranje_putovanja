import { useCallback, useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import type { AdminSystemStats, AdminUserRow } from '../models/admin';
import { adminService } from '../services/adminService';
import { ApiError } from '../services/httpClient';

function formatDate(iso: string): string {
  try {
    return new Date(iso).toLocaleString('sr-Latn', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  } catch {
    return iso;
  }
}

export function AdminUsersPage() {
  const { accessToken, user } = useAuth();
  const [rows, setRows] = useState<AdminUserRow[] | null>(null);
  const [stats, setStats] = useState<AdminSystemStats | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [u, s] = await Promise.all([
        adminService.listUsers(accessToken),
        adminService.getStats(accessToken),
      ]);
      setRows(u);
      setStats(s);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Neuspjelo učitavanje.');
      setRows(null);
      setStats(null);
    } finally {
      setLoading(false);
    }
  }, [accessToken]);

  useEffect(() => {
    void load();
  }, [load]);

  const patch = async (id: string, body: { isActive?: boolean; roleId?: number }) => {
    setBusyId(id);
    setError(null);
    try {
      const updated = await adminService.updateUser(accessToken, id, body);
      setRows((prev) =>
        prev ? prev.map((r) => (r.id === updated.id ? { ...r, ...updated } : r)) : prev
      );
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Ažuriranje nije uspjelo.');
    } finally {
      setBusyId(null);
    }
  };

  return (
    <div className="admin-page">
      <section className="plans-hero">
        <div className="plans-hero-copy">
          <p className="plans-kicker">Administracija</p>
          <h1>Korisnici sistema</h1>
          <p className="plans-lead">
            Pregled naloga, uloga (User / Admin) i statusa aktivnosti. Ne možeš ukloniti Admin ulogu sa sopstvenog
            naloga niti ga deaktivirati.
          </p>
        </div>
      </section>

      {stats ? (
        <div className="admin-stats glass-panel">
          <div>
            <span className="muted">Ukupno korisnika</span>
            <strong>{stats.totalUsers}</strong>
          </div>
          <div>
            <span className="muted">Aktivnih</span>
            <strong>{stats.activeUsers}</strong>
          </div>
          <div>
            <span className="muted">Admina</span>
            <strong>{stats.adminUsers}</strong>
          </div>
        </div>
      ) : null}

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
          <p className="muted">Nema korisnika u bazi.</p>
        </div>
      ) : (
        <div className="card glass-panel admin-table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th>Ime</th>
                <th>Email</th>
                <th>Uloga</th>
                <th>Status</th>
                <th>Kreiran</th>
                <th>Akcije</th>
              </tr>
            </thead>
            <tbody>
              {rows?.map((r) => {
                const self = r.id === user?.id;
                const busy = busyId === r.id;
                return (
                  <tr key={r.id}>
                    <td>
                      {[r.firstName, r.lastName].filter(Boolean).join(' ')}
                      {self ? <span className="muted"> (ti)</span> : null}
                    </td>
                    <td>{r.email}</td>
                    <td>
                      <span className="pill">{r.role}</span>
                    </td>
                    <td>{r.isActive ? <span className="pill">Aktivan</span> : <span className="pill">Neaktivan</span>}</td>
                    <td className="muted">{formatDate(r.createdAtUtc)}</td>
                    <td className="admin-actions">
                      <button
                        type="button"
                        className="btn ghost btn-sm"
                        disabled={busy || (self && r.role === 'Admin')}
                        title={self && r.role === 'Admin' ? 'Ne možeš sebi ukloniti admin ulogu' : undefined}
                        onClick={() =>
                          void patch(r.id, { roleId: r.role === 'Admin' ? 1 : 2 })
                        }
                      >
                        {r.role === 'Admin' ? 'Postavi User' : 'Postavi Admin'}
                      </button>
                      <button
                        type="button"
                        className="btn ghost btn-sm"
                        disabled={busy || (self && r.isActive)}
                        title={self ? 'Ne možeš deaktivirati sopstveni nalog' : undefined}
                        onClick={() => void patch(r.id, { isActive: !r.isActive })}
                      >
                        {r.isActive ? 'Deaktiviraj' : 'Aktiviraj'}
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
