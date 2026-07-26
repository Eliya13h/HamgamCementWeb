import { Suspense } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import ProtectedRoute from '../components/auth/ProtectedRoute'
import DashboardLayout from '../components/layout/DashboardLayout'
import {
  AccessLevelsPage,
  CategoriesPage,
  CurrenciesListPage,
  CustomersPage,
  CustomerDetailPage,
  DashboardPage,
  DriversPage,
  PersonnelPage,
  SalaryPaymentsPage,
  ExchangeHistoryPage,
  ExpenseCategoriesPage,
  ExpensesPage,
  ExpensesReportPage,
  AccountsPage,
  JournalEntriesPage,
  FixedAssetsPage,
  AccountingCategoriesPage,
  EquityTxnsPage,
  CashBoxesPage,
  CashShiftsPage,
  InventoryStockPage,
  JournalPage,
  Login,
  MaintenancePage,
  MeaurmentsPage,
  ProductListPage,
  ProductionReportPage,
  DailyProductionPage,
  ProductionPlanPage,
  ProductionFormulasPage,
  ProductsReportPage,
  PurchasePage,
  RevenuesPage,
  RevenuesReportPage,
  ReportsPage,
  RoutesPage,
  SalePage,
  ShareholdersPage,
  StocktakingHistoryPage,
  WarehouseTransfersPage,
  WarehouseTurnoverPage,
  SuppliersPage,
  SupplierDetailPage,
  TransportInvoicesPage,
  TransportationPage,
  TransportReportPage,
  UsersPage,
  VehicleOwnersPage,
  VehiclesPage,
  VehicleTypesPage,
  WarehousesPage,
  SettingsPage,
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

            <Route path="people/customers" element={<CustomersPage />} />
            <Route path="people/customers/:id" element={<CustomerDetailPage />} />
            <Route path="people/suppliers" element={<SuppliersPage />} />
            <Route path="people/suppliers/:id" element={<SupplierDetailPage />} />
            <Route path="people/personnel" element={<PersonnelPage />} />
            <Route
              path="people/employees"
              element={<Navigate to="/people/personnel?tab=employees" replace />}
            />
            <Route
              path="people/attendance"
              element={<Navigate to="/people/personnel?tab=attendance" replace />}
            />
            <Route
              path="people/departments"
              element={<Navigate to="/people/personnel?tab=departments" replace />}
            />
            <Route path="people/salaries" element={<SalaryPaymentsPage />} />
            <Route path="people/drivers" element={<DriversPage />} />
            <Route path="people/vehicle-owners" element={<VehicleOwnersPage />} />
            <Route path="people/shareholders" element={<ShareholdersPage />} />

            <Route path="currencies/list" element={<CurrenciesListPage />} />
            <Route path="currencies/exchange" element={<ExchangeHistoryPage />} />

            <Route path="production/formulas" element={<ProductionFormulasPage />} />
            <Route path="production/daily" element={<DailyProductionPage />} />
            <Route path="production/plan" element={<ProductionPlanPage />} />

            <Route path="transport/shipping" element={<TransportationPage />} />
            <Route path="transport/routes" element={<RoutesPage />} />
            <Route path="transport/vehicles" element={<VehiclesPage />} />
            <Route path="transport/vehicle-types" element={<VehicleTypesPage />} />
            <Route path="transport/maintenance" element={<MaintenancePage />} />
            <Route path="transport/invoices" element={<TransportInvoicesPage />} />
            <Route path="transport/expense-categories" element={<ExpenseCategoriesPage />} />

            <Route path="transactions/purchase" element={<PurchasePage />} />
            <Route path="transactions/sale" element={<SalePage />} />

            <Route path="products/list" element={<ProductListPage />} />
            <Route path="products/categories" element={<CategoriesPage />} />
            <Route path="products/meaurments" element={<MeaurmentsPage />} />

            <Route path="inventory/warehouses" element={<WarehousesPage />} />
            <Route path="inventory/stock" element={<InventoryStockPage />} />
            <Route path="inventory/turnover" element={<WarehouseTurnoverPage />} />
            <Route path="inventory/transfers" element={<WarehouseTransfersPage />} />
            <Route path="inventory/stocktaking" element={<StocktakingHistoryPage />} />

            <Route path="accounting/accounts" element={<AccountsPage />} />
            <Route path="accounting/journal-entries" element={<JournalEntriesPage />} />
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
            <Route path="cash/shifts" element={<CashShiftsPage />} />

            <Route path="reporting/products" element={<ProductsReportPage />} />
            <Route path="reporting/production" element={<ProductionReportPage />} />
            <Route path="reporting/transport" element={<TransportReportPage />} />
            <Route path="reporting/revenues" element={<RevenuesReportPage />} />
            <Route path="reporting/expenses" element={<ExpensesReportPage />} />
            <Route path="reporting/journal" element={<JournalPage />} />

            <Route path="users/list" element={<UsersPage />} />
            <Route path="users/roles" element={<AccessLevelsPage />} />

            <Route path="settings" element={<SettingsPage />} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Route>
        </Route>
      </Routes>
    </Suspense>
  )
}

export default AppRoutes
