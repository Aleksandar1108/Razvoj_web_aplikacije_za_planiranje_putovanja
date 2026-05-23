import { Navigate, Route, Routes } from 'react-router-dom';
import { AppHeader } from './components/AppHeader';
import { AdminRoute } from './components/AdminRoute';
import { ProtectedRoute } from './components/ProtectedRoute';
import { HomePage } from './pages/HomePage';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { TravelActivityFormPage } from './pages/TravelActivityFormPage';
import { TravelExpenseFormPage } from './pages/TravelExpenseFormPage';
import { TravelPlanDetailPage } from './pages/TravelPlanDetailPage';
import { TravelDestinationFormPage } from './pages/TravelDestinationFormPage';
import { TravelPlanFormPage } from './pages/TravelPlanFormPage';
import { TravelPlansListPage } from './pages/TravelPlansListPage';
import { SharedPlansPage } from './pages/SharedPlansPage';
import { ImportShareQrPage } from './pages/ImportShareQrPage';
import { AdminUsersPage } from './pages/AdminUsersPage';

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
            <Route path="/shared-plans" element={<SharedPlansPage />} />
            <Route path="/share/qr" element={<ImportShareQrPage />} />
            <Route path="/plans/new" element={<TravelPlanFormPage />} />
            <Route path="/plans/:planId/edit" element={<TravelPlanFormPage />} />
            <Route path="/plans/:planId/destinations/new" element={<TravelDestinationFormPage />} />
            <Route path="/plans/:planId/destinations/:destinationId/edit" element={<TravelDestinationFormPage />} />
            <Route path="/plans/:planId/activities/new" element={<TravelActivityFormPage />} />
            <Route path="/plans/:planId/activities/:activityId/edit" element={<TravelActivityFormPage />} />
            <Route path="/plans/:planId/expenses/new" element={<TravelExpenseFormPage />} />
            <Route path="/plans/:planId/expenses/:expenseId/edit" element={<TravelExpenseFormPage />} />
            <Route path="/plans/:planId" element={<TravelPlanDetailPage />} />
            <Route element={<AdminRoute />}>
              <Route path="/admin/korisnici" element={<AdminUsersPage />} />
            </Route>
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>
    </div>
  );
}
