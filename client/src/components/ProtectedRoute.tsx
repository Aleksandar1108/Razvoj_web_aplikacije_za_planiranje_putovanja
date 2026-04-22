import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export function ProtectedRoute() {
  const { accessToken, bootstrapping } = useAuth();

  if (bootstrapping) {
    return (
      <div className="page">
        <p className="muted">Učitavanje sesije…</p>
      </div>
    );
  }

  if (!accessToken) {
    return <Navigate to="/login" replace />;
  }

  return <Outlet />;
}
