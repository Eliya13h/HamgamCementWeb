import { Navigate, Route, Routes } from 'react-router-dom'
import DashboardLayout from '../components/layout/DashboardLayout'
import DashboardPage from '../pages/Dashboard/DashboardPage'

function PlaceholderPage({ title }) {
  return (
    <div className="content-card card border-0">
      <div className="card-body p-4">
        <h2 className="card-title mb-2">{title}</h2>
        <p className="text-muted mb-0">محتوای این صفحه به‌زودی اضافه می‌شود.</p>
      </div>
    </div>
  )
}

function AppRoutes() {
  return (
    <Routes>
      <Route element={<DashboardLayout />}>
        <Route index element={<DashboardPage />} />
        <Route path="reports" element={<PlaceholderPage title="آمار و تحلیل" />} />

        <Route path="employees/org-chart" element={<PlaceholderPage title="چارت سازمانی" />} />
        <Route path="employees/manage" element={<PlaceholderPage title="مدیریت کارمندان" />} />

        <Route path="production/daily" element={<PlaceholderPage title="گزارش روزانه تولید" />} />
        <Route path="production/plan" element={<PlaceholderPage title="برنامه تولید" />} />

        <Route path="sales/orders" element={<PlaceholderPage title="سفارشات" />} />
        <Route path="sales/customers" element={<PlaceholderPage title="مشتریان" />} />

        <Route path="inventory/stock" element={<PlaceholderPage title="موجودی انبار" />} />
        <Route path="inventory/transfers" element={<PlaceholderPage title="انتقالات انبار" />} />

        <Route path="settings" element={<PlaceholderPage title="تنظیمات" />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  )
}

export default AppRoutes
