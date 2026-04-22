import { Navigate, Route, Routes } from 'react-router-dom';
import { AppHeader } from './components/AppHeader';
import { ProtectedRoute } from './components/ProtectedRoute';
import { HomePage } from './pages/HomePage';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { TravelPlanDetailPage } from './pages/TravelPlanDetailPage';
import { TravelPlanFormPage } from './pages/TravelPlanFormPage';
import { TravelPlansListPage } from './pages/TravelPlansListPage';

export default function App() {
  return (
    <div className="app-shell">
      <AppHeader />
      <main className="main">
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route element={<ProtectedRoute />}>
            <Route path="/" element={<HomePage />} />
            <Route path="/plans" element={<TravelPlansListPage />} />
            <Route path="/plans/new" element={<TravelPlanFormPage />} />
            <Route path="/plans/:planId/edit" element={<TravelPlanFormPage />} />
            <Route path="/plans/:planId" element={<TravelPlanDetailPage />} />
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>
    </div>
  );
}
