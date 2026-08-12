import { Suspense } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import ProtectedRoute from '../components/auth/ProtectedRoute'
import DashboardLayout from '../components/layout/DashboardLayout'
import {
  AccessLevelsPage,
  CurrenciesListPage,
  CustomersPage,
  CustomerDetailPage,
  DashboardPage,
  ExchangeHistoryPage,
  ExpensesPage,
  ExpensesReportPage,
  AccountsPage,
  JournalEntriesPage,
  FixedAssetsPage,
  AccountingCategoriesPage,
  EquityTxnsPage,
  CashBoxesPage,
  CashShiftsPage,
  BankAccountsPage,
  PartySettlementsPage,
  CurrencyExchangePage,
  CostCentersPage,
  DoubtfulProvisionsPage,
  RecurringJournalsPage,
  PettyCashPage,
  JournalPage,
  Login,
  RevenuesPage,
  RevenuesReportPage,
  ReportsPage,
  UsersPage,
  SettingsPage,
  VehiclesPage,
  VehiclePairsPage,
  VehicleTypesPage,
  TripExpenseCategoriesPage,
  VehicleOwnersPage,
  DriversPage,
  TripsPage,
  FleetReportsPage,
} from './lazyPages'

function PageLoader() {
  return (
    <div className="d-flex justify-content-center align-items-center p-5 min-vh-50">
      <div className="spinner-border text-primary" role="status" aria-label="در حال بارگذاری" />
    </div>
  )
}

function AppRoutes() {
  return (
    <Suspense fallback={<PageLoader />}>
      <Routes>
        <Route path="/login" element={<Login />} />

        <Route element={<ProtectedRoute />}>
          <Route element={<DashboardLayout />}>
            <Route index element={<DashboardPage />} />
            <Route path="reports" element={<ReportsPage />} />

            <Route path="transport/vehicle-types" element={<VehicleTypesPage />} />
            <Route path="transport/vehicles" element={<VehiclesPage />} />
            <Route path="transport/vehicle-pairs" element={<VehiclePairsPage />} />
            <Route path="transport/trips" element={<TripsPage />} />
            <Route path="transport/trip-expense-categories" element={<TripExpenseCategoriesPage />} />

            <Route path="people/customers" element={<CustomersPage />} />
            <Route path="people/customers/:id" element={<CustomerDetailPage />} />
            <Route path="people/drivers" element={<DriversPage />} />
            <Route path="people/vehicle-owners" element={<VehicleOwnersPage />} />

            <Route path="currencies/list" element={<CurrenciesListPage />} />
            <Route path="currencies/exchange" element={<ExchangeHistoryPage />} />

            <Route path="accounting/accounts" element={<AccountsPage />} />
            <Route path="accounting/journal-entries" element={<JournalEntriesPage />} />
            <Route path="accounting/settlements" element={<PartySettlementsPage />} />
            <Route path="accounting/currency-exchange" element={<CurrencyExchangePage />} />
            <Route path="accounting/cost-centers" element={<CostCentersPage />} />
            <Route path="accounting/doubtful-provisions" element={<DoubtfulProvisionsPage />} />
            <Route path="accounting/recurring-journals" element={<RecurringJournalsPage />} />
            <Route path="accounting/equity" element={<EquityTxnsPage />} />
            <Route path="accounting/fixed-assets" element={<FixedAssetsPage />} />
            <Route path="accounting/revenues" element={<RevenuesPage />} />
            <Route path="accounting/expenses" element={<ExpensesPage />} />
            <Route path="accounting/categories" element={<AccountingCategoriesPage />} />
            <Route
              path="accounting/expense-categories"
              element={<Navigate to="/accounting/categories?tab=expenses" replace />}
            />
            <Route
              path="accounting/revenue-categories"
              element={<Navigate to="/accounting/categories?tab=revenues" replace />}
            />
            <Route
              path="accounting/fixed-asset-categories"
              element={<Navigate to="/accounting/categories?tab=fixed-assets" replace />}
            />

            <Route path="cash/boxes" element={<CashBoxesPage />} />
            <Route path="cash/banks" element={<BankAccountsPage />} />
            <Route path="cash/shifts" element={<CashShiftsPage />} />
            <Route path="cash/petty-cash" element={<PettyCashPage />} />

            <Route path="reporting/fleet" element={<FleetReportsPage />} />
            <Route path="reporting/revenues" element={<RevenuesReportPage />} />
            <Route path="reporting/expenses" element={<ExpensesReportPage />} />
            <Route path="reporting/journal" element={<JournalPage />} />

            <Route path="users/list" element={<UsersPage />} />
            <Route path="users/roles" element={<AccessLevelsPage />} />

            <Route path="settings" element={<SettingsPage />} />

            {/* مسیرهای قدیمی سیمان — هدایت به داشبورد */}
            <Route path="people/suppliers/*" element={<Navigate to="/" replace />} />
            <Route path="production/*" element={<Navigate to="/" replace />} />
            <Route path="transactions/*" element={<Navigate to="/" replace />} />
            <Route path="products/*" element={<Navigate to="/" replace />} />
            <Route path="inventory/*" element={<Navigate to="/" replace />} />
            <Route path="transport/routes" element={<Navigate to="/transport/trips" replace />} />
            <Route path="transport/invoices" element={<Navigate to="/transport/trips" replace />} />

            <Route path="*" element={<Navigate to="/" replace />} />
          </Route>
        </Route>
      </Routes>
    </Suspense>
  )
}

export default AppRoutes
