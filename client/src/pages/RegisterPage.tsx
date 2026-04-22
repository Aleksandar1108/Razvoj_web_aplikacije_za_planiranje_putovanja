import { useState, type FormEvent } from 'react';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { ApiError } from '../services/httpClient';

export function RegisterPage() {
  const { accessToken, register } = useAuth();
  const navigate = useNavigate();
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
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

    if (password.length < 8) {
      setError('Lozinka mora imati najmanje 8 karaktera.');
      return;
    }

    setPending(true);
    try {
      await register({
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        email: email.trim(),
        password,
      });
      navigate('/', { replace: true });
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Došlo je do greške.');
    } finally {
      setPending(false);
    }
  }

  return (
    <div className="page narrow">
      <h1>Registracija</h1>
      <p className="muted">Kreira se nalog sa ulogom korisnika (User).</p>

      <form className="card form" onSubmit={onSubmit}>
        <label>
          Ime
          <input
            type="text"
            autoComplete="given-name"
            value={firstName}
            onChange={(e) => setFirstName(e.target.value)}
            required
            maxLength={100}
          />
        </label>
        <label>
          Prezime
          <input
            type="text"
            autoComplete="family-name"
            value={lastName}
            onChange={(e) => setLastName(e.target.value)}
            required
            maxLength={100}
          />
        </label>
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
          Lozinka (min. 8)
          <input
            type="password"
            autoComplete="new-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            minLength={8}
            maxLength={128}
          />
        </label>

        {error ? <p className="error">{error}</p> : null}

        <button type="submit" className="btn primary" disabled={pending}>
          {pending ? 'Registracija…' : 'Registruj se'}
        </button>
      </form>

      <p className="muted">
        Već imaš nalog? <Link to="/login">Prijavi se</Link>
      </p>
    </div>
  );
}
