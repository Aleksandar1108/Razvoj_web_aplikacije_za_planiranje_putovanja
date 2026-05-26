import { useState, type FormEvent } from 'react';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { ApiError } from '../services/httpClient';

export function LoginPage() {
  const { accessToken, login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  if (accessToken) {
    return <Navigate to="/" replace />;
  }

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setPending(true);
    try {
      await login({ email: email.trim(), password });
      navigate('/', { replace: true });
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Došlo je do greške.');
    } finally {
      setPending(false);
    }
  }

  return (
    <div className="auth-page">
      <div className="auth-page-bg" aria-hidden />
      <div className="page narrow auth-panel">
        <p className="auth-kicker">Planiranje putovanja</p>
        <h1 className="auth-title">Prijava</h1>
        <p className="muted auth-lead">Unesi email i lozinku da pristupiš svojim planovima.</p>

        <form className="card form glass-panel auth-form" onSubmit={onSubmit}>
        <label>
          Email
          <input
            type="email"
            autoComplete="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
        </label>
        <label>
          Lozinka
          <input
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            minLength={8}
          />
        </label>

        {error ? <p className="error">{error}</p> : null}

        <button type="submit" className="btn btn-glow primary btn-lg auth-submit" disabled={pending}>
          {pending ? 'Prijava…' : 'Prijavi se'}
        </button>
      </form>

      <p className="muted auth-footer">
        Nemaš nalog? <Link to="/register">Registruj se</Link>
      </p>
      <p className="muted auth-footer" style={{ marginTop: 12 }}>
        <Link to="/share/view-qr" className="btn ghost">
          Učitaj QR
        </Link>
      </p>
      </div>
    </div>
  );
}
