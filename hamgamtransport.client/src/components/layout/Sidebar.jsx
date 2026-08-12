import { useMemo } from 'react'
import { NavLink } from 'react-router-dom'
import Icon from '../common/Icon'
import { navItems, settingsNavItem } from '../../config/navigation'
import { usePermission } from '../../permissions/usePermission'
import { filterNavByPermission, pathPermission } from '../../permissions/utils'
import SidebarNavItem from './SidebarNavItem'

function Sidebar({ collapsed, onNavigate, isMobile }) {
  const isCollapsed = collapsed && !isMobile
  const { can } = usePermission()

  const visibleNavItems = useMemo(() => filterNavByPermission(navItems, can), [can])
  const showSettings = can(pathPermission(settingsNavItem.path, 'view'))

  return (
    <aside
      className={`dashboard-sidebar d-flex flex-column ${isCollapsed ? 'is-collapsed' : ''}`}
    >
      <div className="sidebar-brand d-flex align-items-center gap-2">
        <div className="brand-icon">
          <Icon name="building" />
        </div>
        <div className={`brand-text ${isCollapsed ? 'brand-text-hidden' : ''}`}>
          <span className="brand-title">همگام ترانسپورت</span>
          <span className="brand-subtitle">بخش مدیریت</span>
        </div>
      </div>

      <nav className="sidebar-nav flex-grow-1 px-2 py-2 hc-scroll">
        <ul className="nav flex-column gap-1">
          {visibleNavItems.map((item) => (
            <SidebarNavItem
              key={item.id ?? item.path}
              item={item}
              isCollapsed={isCollapsed}
              onNavigate={onNavigate}
            />
          ))}
        </ul>
      </nav>

      {showSettings && (
        <div className="sidebar-footer px-2 pb-3">
          <NavLink
            to={settingsNavItem.path}
            className={({ isActive }) =>
              `nav-link d-flex align-items-center ${isActive ? 'active' : ''}`
            }
            title={isCollapsed ? settingsNavItem.label : undefined}
            onClick={onNavigate}
          >
            <Icon name={settingsNavItem.icon} className="nav-icon" />
            <span className={`nav-label ${isCollapsed ? 'nav-label-hidden' : ''}`}>
              {settingsNavItem.label}
            </span>
          </NavLink>
        </div>
      )}
    </aside>
  )
}

export default Sidebar
