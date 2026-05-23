import { Link, NavLink } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export function AppHeader() {
  const { accessToken, user, logout } = useAuth();

  return (
    <header className="header header-elevated">
      <Link to="/" className="brand">
        Planiranje putovanja
      </Link>
      <nav className="nav">
        {!accessToken ? (
          <>
            <NavLink to="/login" className={({ isActive }) => (isActive ? 'active' : '')}>
              Prijava
            </NavLink>
            <NavLink to="/register" className={({ isActive }) => (isActive ? 'active' : '')}>
              Registracija
            </NavLink>
          </>
        ) : (
          <>
            <NavLink to="/plans" className={({ isActive }) => (isActive ? 'active' : '')}>
              Planovi
            </NavLink>
            <NavLink to="/shared-plans" className={({ isActive }) => (isActive ? 'active' : '')}>
              Deljeni planovi
            </NavLink>
            <NavLink to="/share/qr" className={({ isActive }) => (isActive ? 'active' : '')}>
              Učitaj QR
            </NavLink>
            {user?.role === 'Admin' ? (
              <NavLink to="/admin/korisnici" className={({ isActive }) => (isActive ? 'active' : '')}>
                Admin
              </NavLink>
            ) : null}
            <span className="muted">
              {[user?.firstName, user?.lastName].filter(Boolean).join(' ')}{' '}
              <span className="pill">{user?.role}</span>
            </span>
            <button type="button" className="btn ghost" onClick={logout}>
              Odjava
            </button>
          </>
        )}
      </nav>
    </header>
  );
}
