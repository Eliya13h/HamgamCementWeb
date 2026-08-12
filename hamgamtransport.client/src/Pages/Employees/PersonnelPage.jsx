import { useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { usePermission } from '../../permissions/usePermission'
import { pathPermission } from '../../permissions/utils'
import AttendancePage from './AttendancePage'
import DepartmentsPage from './DepartmentsPage'
import EmployeesPage from './EmployeesPage'

const TABS = [
  {
    id: 'employees',
    label: 'کارمندان',
    permissionPath: '/people/employees',
    Component: EmployeesPage,
  },
  {
    id: 'attendance',
    label: 'حضور و غیاب',
    permissionPath: '/people/attendance',
    Component: AttendancePage,
  },
  {
    id: 'departments',
    label: 'بخش‌ها',
    permissionPath: '/people/departments',
    Component: DepartmentsPage,
  },
]

function PersonnelPage() {
  const { can } = usePermission()
  const [searchParams, setSearchParams] = useSearchParams()

  const visibleTabs = useMemo(
    () => TABS.filter((tab) => can(pathPermission(tab.permissionPath, 'view'))),
    [can],
  )

  const requestedTab = searchParams.get('tab')
  const [activeTab, setActiveTab] = useState(
    () => visibleTabs.find((t) => t.id === requestedTab)?.id ?? visibleTabs[0]?.id,
  )

  useEffect(() => {
    if (!visibleTabs.length) return
    if (!visibleTabs.some((t) => t.id === activeTab)) {
      setActiveTab(visibleTabs[0].id)
    }
  }, [visibleTabs, activeTab])

  useEffect(() => {
    if (!activeTab) return
    if (searchParams.get('tab') === activeTab) return
    setSearchParams({ tab: activeTab }, { replace: true })
  }, [activeTab, searchParams, setSearchParams])

  const tab = visibleTabs.find((t) => t.id === activeTab)

  if (!tab) {
    return (
      <div className="content-card card border-0 h-100">
        <div className="card-body p-4 text-muted">دسترسی به هیچ بخشی از پرسونل ندارید.</div>
      </div>
    )
  }

  const TabComponent = tab.Component

  return (
    <div className="content-card card border-0 h-100">
      <div className="card-header bg-transparent border-0 pt-3 px-4 pb-0">
        <ul className="nav nav-tabs card-header-tabs">
          {visibleTabs.map((t) => (
            <li className="nav-item" key={t.id}>
              <button
                type="button"
                className={`nav-link ${t.id === activeTab ? 'active' : ''}`}
                onClick={() => setActiveTab(t.id)}
              >
                {t.label}
              </button>
            </li>
          ))}
        </ul>
      </div>

      <TabComponent key={tab.id} embedded />
    </div>
  )
}

export default PersonnelPage
