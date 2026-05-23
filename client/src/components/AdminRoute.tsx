import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export function AdminRoute() {
  const { accessToken, bootstrapping, user } = useAuth();

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

  if (user?.role !== 'Admin') {
    return <Navigate to="/" replace />;
  }

  return <Outlet />;
}
