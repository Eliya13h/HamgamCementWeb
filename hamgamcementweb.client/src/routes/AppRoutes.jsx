import { Navigate, Route, Routes } from 'react-router-dom'
import ProtectedRoute from '../components/auth/ProtectedRoute'
import Login from '../Pages/Auth/Login'
import DashboardLayout from '../components/layout/DashboardLayout'
import DashboardPage from '../pages/Dashboard/DashboardPage'
import CustomersPage from '../pages/Customers/CustomersPage'
import SuppliersPage from '../pages/Suppliers/SuppliersPage'
import EmployeesPage from '../pages/Employees/EmployeesPage'
import ShareholdersPage from '../pages/Shareholders/ShareholdersPage'
import CurrenciesListPage from '../pages/Finance/CurrenciesListPage'
import ExchangeHistoryPage from '../pages/Finance/ExchangeHistoryPage'
import TransportationPage from '../pages/Transport/TransportationPage'
import VehiclesPage from '../pages/Transport/VehiclesPage'
import MaintenancePage from '../pages/Transport/MaintenancePage'
import PurchasePage from '../pages/Transactions/PurchasePage'
import SalePage from '../pages/Transactions/SalePage'
import ProductListPage from '../pages/Products/ProductListPage'
import RevenuesPage from '../pages/Accounting/RevenuesPage'
import ExpensesPage from '../pages/Accounting/ExpensesPage'
import ProductsReportPage from '../pages/Reporting/ProductsReportPage'
import ProductionReportPage from '../pages/Reporting/ProductionReportPage'
import TransportReportPage from '../pages/Reporting/TransportReportPage'
import RevenuesReportPage from '../pages/Reporting/RevenuesReportPage'
import ExpensesReportPage from '../pages/Reporting/ExpensesReportPage'
import JournalPage from '../pages/Reporting/JournalPage'
import UsersPage from '../pages/Users/UsersPage'
import AccessLevelsPage from '../pages/Users/AccessLevelsPage'

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
      <Route path="/login" element={<Login />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<DashboardLayout />}>
        <Route index element={<DashboardPage />} />
        <Route path="reports" element={<PlaceholderPage title="آمار و تحلیل" />} />

        <Route path="people/customers" element={<CustomersPage />} />
        <Route path="people/suppliers" element={<SuppliersPage />} />
        <Route path="people/employees" element={<EmployeesPage />} />
        <Route path="people/shareholders" element={<ShareholdersPage />} />

        <Route path="currencies/list" element={<CurrenciesListPage />} />
        <Route path="currencies/exchange" element={<ExchangeHistoryPage />} />

        <Route path="production/daily" element={<PlaceholderPage title="گزارش روزانه تولید" />} />
        <Route path="production/plan" element={<PlaceholderPage title="برنامه تولید" />} />

        <Route path="transport/shipping" element={<TransportationPage />} />
        <Route path="transport/vehicles" element={<VehiclesPage />} />
        <Route path="transport/maintenance" element={<MaintenancePage />} />

        <Route path="transactions/purchase" element={<PurchasePage />} />
        <Route path="transactions/sale" element={<SalePage />} />

        <Route path="products/list" element={<ProductListPage />} />

        <Route path="inventory/stock" element={<PlaceholderPage title="موجودی انبار" />} />
        <Route path="inventory/transfers" element={<PlaceholderPage title="انتقالات انبار" />} />

        <Route path="accounting/revenues" element={<RevenuesPage />} />
        <Route path="accounting/expenses" element={<ExpensesPage />} />

        <Route path="reporting/products" element={<ProductsReportPage />} />
        <Route path="reporting/production" element={<ProductionReportPage />} />
        <Route path="reporting/transport" element={<TransportReportPage />} />
        <Route path="reporting/revenues" element={<RevenuesReportPage />} />
        <Route path="reporting/expenses" element={<ExpensesReportPage />} />
        <Route path="reporting/journal" element={<JournalPage />} />

        <Route path="users/list" element={<UsersPage />} />
        <Route path="users/roles" element={<AccessLevelsPage />} />

        <Route path="settings" element={<PlaceholderPage title="تنظیمات" />} />
        <Route path="*" element={<Navigate to="/" replace />} />
        </Route>
      </Route>
    </Routes>
  )
}

export default AppRoutes
