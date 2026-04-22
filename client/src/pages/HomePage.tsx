import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export function HomePage() {
  const { user } = useAuth();

  return (
    <div className="page">
      <h1>Dobrodošao/la</h1>
      <div className="card">
        <p>
          Ulogovan/a si kao <strong>{[user?.firstName, user?.lastName].filter(Boolean).join(' ')}</strong> (
          {user?.email}).
        </p>
        <p className="muted">
          Uloga: <span className="pill">{user?.role}</span>
        </p>
        <p className="muted">Kreiraj i vodi planove putovanja u zasebnom mikroservisu (TravelPlansApi).</p>
        <p>
          <Link to="/plans" className="btn primary">
            Otvori planove putovanja
          </Link>
        </p>
      </div>
    </div>
  );
}
