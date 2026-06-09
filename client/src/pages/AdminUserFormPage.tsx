import { useEffect, useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import type { CreateAdminUserRequest, UpdateAdminUserRequest } from '../models/admin';
import { adminService } from '../services/adminService';
import { ApiError } from '../services/httpClient';

type FormState = {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  roleId: number;
  isActive: boolean;
};

function emptyForm(): FormState {
  return {
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    roleId: 1,
    isActive: true,
  };
}

export function AdminUserFormPage() {
  const { userId } = useParams<{ userId: string }>();
  const isEdit = Boolean(userId);
  const navigate = useNavigate();
  const { accessToken, user: currentUser } = useAuth();

  const [form, setForm] = useState<FormState>(emptyForm);
  const [loading, setLoading] = useState(isEdit);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isSelf = isEdit && userId === currentUser?.id;

  useEffect(() => {
    if (!isEdit || !userId) return;
    let cancelled = false;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const u = await adminService.getUser(accessToken, userId);
        if (cancelled) return;
        setForm({
          firstName: u.firstName,
          lastName: u.lastName,
          email: u.email,
          password: '',
          roleId: u.role === 'Admin' ? 2 : 1,
          isActive: u.isActive,
        });
      } catch (e) {
        if (!cancelled) setError(e instanceof ApiError ? e.message : 'Korisnik nije pronađen.');
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [accessToken, isEdit, userId]);

  async function onSubmit(ev: FormEvent) {
    ev.preventDefault();
    setSaving(true);
    setError(null);

    const firstName = form.firstName.trim();
    const lastName = form.lastName.trim();
    const email = form.email.trim();

    if (!firstName || !lastName || !email) {
      setError('Ime, prezime i email su obavezni.');
      setSaving(false);
      return;
    }

    if (!isEdit && form.password.length < 8) {
      setError('Lozinka mora imati najmanje 8 karaktera.');
      setSaving(false);
      return;
    }

    if (isEdit && form.password.length > 0 && form.password.length < 8) {
      setError('Nova lozinka mora imati najmanje 8 karaktera.');
      setSaving(false);
      return;
    }

    try {
      if (isEdit && userId) {
        const body: UpdateAdminUserRequest = {
          firstName,
          lastName,
          email,
          roleId: form.roleId,
          isActive: form.isActive,
        };
        if (form.password.trim()) body.password = form.password;
        await adminService.updateUser(accessToken, userId, body);
      } else {
        const body: CreateAdminUserRequest = {
          firstName,
          lastName,
          email,
          password: form.password,
          roleId: form.roleId,
          isActive: form.isActive,
        };
        await adminService.createUser(accessToken, body);
      }
      navigate('/admin/korisnici', { replace: true });
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Čuvanje nije uspelo.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="admin-page">
      <section className="plans-hero">
        <div className="plans-hero-copy">
          <p className="plans-kicker">Administracija</p>
          <h1>{isEdit ? 'Izmena korisnika' : 'Novi korisnik'}</h1>
          <p className="plans-lead">
            {isEdit
              ? 'Ažuriraj profil, ulogu i status naloga. Lozinku menjaj samo ako želiš novu.'
              : 'Kreiraj nalog sa početnom lozinkom, ulogom i statusom aktivnosti.'}
          </p>
          <div className="form-actions" style={{ marginTop: '1rem' }}>
            <Link to="/admin/korisnici" className="btn ghost">
              ← Nazad na korisnike
            </Link>
          </div>
        </div>
      </section>

      {loading ? (
        <div className="card glass-panel plans-loading">Učitavanje…</div>
      ) : (
        <form className="card form glass-panel narrow-form" onSubmit={onSubmit}>
          <label>
            Ime
            <input
              type="text"
              value={form.firstName}
              onChange={(e) => setForm((f) => ({ ...f, firstName: e.target.value }))}
              required
              maxLength={100}
            />
          </label>
          <label>
            Prezime
            <input
              type="text"
              value={form.lastName}
              onChange={(e) => setForm((f) => ({ ...f, lastName: e.target.value }))}
              required
              maxLength={100}
            />
          </label>
          <label>
            Email
            <input
              type="email"
              value={form.email}
              onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))}
              required
              maxLength={256}
            />
          </label>
          <label>
            {isEdit ? 'Nova lozinka (opciono)' : 'Lozinka'}
            <input
              type="password"
              value={form.password}
              onChange={(e) => setForm((f) => ({ ...f, password: e.target.value }))}
              required={!isEdit}
              minLength={isEdit ? undefined : 8}
              autoComplete={isEdit ? 'new-password' : 'off'}
            />
          </label>
          <label>
            Uloga
            <select
              value={form.roleId}
              disabled={isSelf}
              title={isSelf ? 'Ne možeš sebi ukloniti admin ulogu' : undefined}
              onChange={(e) => setForm((f) => ({ ...f, roleId: Number(e.target.value) }))}
            >
              <option value={1}>User</option>
              <option value={2}>Admin</option>
            </select>
          </label>
          <label className="checkbox-label">
            <input
              type="checkbox"
              checked={form.isActive}
              disabled={isSelf}
              title={isSelf ? 'Ne možeš deaktivirati sopstveni nalog' : undefined}
              onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.checked }))}
            />
            Nalog je aktivan
          </label>

          {error ? <p className="error">{error}</p> : null}

          <div className="form-actions">
            <button type="submit" className="btn btn-glow primary" disabled={saving}>
              {saving ? 'Čuvam…' : isEdit ? 'Sačuvaj izmene' : 'Kreiraj korisnika'}
            </button>
            <Link to="/admin/korisnici" className="btn ghost">
              Otkaži
            </Link>
          </div>
        </form>
      )}
    </div>
  );
}
