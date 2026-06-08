import { useEffect, useState } from 'react'
import { Outlet } from 'react-router-dom'
import Header from './Header'
import Sidebar from './Sidebar'

function DashboardLayout() {
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false)
  const [mobileSidebarOpen, setMobileSidebarOpen] = useState(false)
  const [isMobile, setIsMobile] = useState(false)

  useEffect(() => {
    const mediaQuery = window.matchMedia('(max-width: 991.98px)')

    const handleChange = (event) => {
      setIsMobile(event.matches)
      if (event.matches) {
        setMobileSidebarOpen(false)
      }
    }

    handleChange(mediaQuery)
    mediaQuery.addEventListener('change', handleChange)
    return () => mediaQuery.removeEventListener('change', handleChange)
  }, [])

  useEffect(() => {
    document.body.classList.toggle('sidebar-drawer-open', mobileSidebarOpen)
    return () => document.body.classList.remove('sidebar-drawer-open')
  }, [mobileSidebarOpen])

  const isSidebarExpanded = isMobile ? mobileSidebarOpen : !sidebarCollapsed

  const handleSidebarToggle = () => {
    if (isMobile) {
      setMobileSidebarOpen((prev) => !prev)
      return
    }
    setSidebarCollapsed((prev) => !prev)
  }

  const closeMobileSidebar = () => setMobileSidebarOpen(false)

  return (
    <div
      className={`dashboard-layout ${sidebarCollapsed && !isMobile ? 'is-sidebar-collapsed' : ''}`}
    >
      {mobileSidebarOpen && (
        <button
          type="button"
          className="sidebar-backdrop d-lg-none"
          onClick={closeMobileSidebar}
          aria-label="بستن منو"
        />
      )}

      <aside
        className={`sidebar-wrapper ${mobileSidebarOpen ? 'is-open' : ''}`}
        aria-hidden={isMobile && !mobileSidebarOpen}
      >
        <Sidebar
          collapsed={sidebarCollapsed}
          onNavigate={isMobile ? closeMobileSidebar : undefined}
          isMobile={isMobile}
        />
      </aside>

      <div className="dashboard-main">
        <Header
          isSidebarExpanded={isSidebarExpanded}
          onSidebarToggle={handleSidebarToggle}
        />
        <main className="dashboard-content hc-scroll">
          <Outlet />
        </main>
      </div>
    </div>
  )
}

export default DashboardLayout
