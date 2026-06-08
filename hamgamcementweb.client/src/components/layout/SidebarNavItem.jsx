import { useEffect, useState } from 'react'
import { NavLink, useLocation } from 'react-router-dom'
import Icon from '../common/Icon'
import { isChildActive, isNavGroup } from '../../config/navigation'

function SidebarNavItem({ item, isCollapsed, onNavigate }) {
  const location = useLocation()

  if (!isNavGroup(item)) {
    return (
      <li className="nav-item">
        <NavLink
          to={item.path}
          end={item.path === '/'}
          className={({ isActive }) =>
            `nav-link d-flex align-items-center ${isActive ? 'active' : ''}`
          }
          title={isCollapsed ? item.label : undefined}
          onClick={onNavigate}
        >
          <Icon name={item.icon} className="nav-icon" />
          <span className={`nav-label ${isCollapsed ? 'nav-label-hidden' : ''}`}>
            {item.label}
          </span>
        </NavLink>
      </li>
    )
  }

  const childActive = isChildActive(location.pathname, item.children)
  const [expanded, setExpanded] = useState(childActive)
  const showCollapsedSubs = isCollapsed && (childActive || expanded)

  useEffect(() => {
    if (childActive) {
      setExpanded(true)
    }
  }, [childActive, location.pathname])

  const toggleExpanded = () => {
    setExpanded((prev) => !prev)
  }

  return (
    <li
      className={`nav-item nav-group ${expanded && !isCollapsed ? 'is-expanded' : ''} ${
        childActive ? 'has-active-descendant' : ''
      } ${showCollapsedSubs ? 'is-collapsed-open' : ''}`}
    >
      <button
        type="button"
        className={`nav-link nav-link-parent d-flex align-items-center w-100 border-0 ${
          childActive ? 'has-active-child' : ''
        }`}
        onClick={toggleExpanded}
        title={isCollapsed ? item.label : undefined}
        aria-expanded={expanded}
      >
        <Icon name={item.icon} className="nav-icon" />
        <span className={`nav-label flex-grow-1 text-start ${isCollapsed ? 'nav-label-hidden' : ''}`}>
          {item.label}
        </span>
        {!isCollapsed && (
          <Icon
            name={expanded ? 'chevron-up' : 'chevron-down'}
            className="nav-chevron"
          />
        )}
      </button>

      {!isCollapsed && expanded && (
        <ul className="nav-submenu list-unstyled mb-0">
          {item.children.map((child) => (
            <li key={child.path} className="nav-sub-item">
              <NavLink
                to={child.path}
                className={({ isActive }) =>
                  `nav-sub-link ${isActive ? 'active' : ''}`
                }
                onClick={onNavigate}
              >
                {child.label}
              </NavLink>
            </li>
          ))}
        </ul>
      )}

      {showCollapsedSubs && (
        <ul className="nav-submenu-collapsed list-unstyled mb-0">
          {item.children.map((child) => (
            <li key={child.path} className="nav-sub-item-collapsed">
              <NavLink
                to={child.path}
                className={({ isActive }) =>
                  `nav-sub-link-icon ${isActive ? 'active' : ''}`
                }
                title={child.label}
                onClick={onNavigate}
              >
                <Icon name="submenu-dot" />
              </NavLink>
            </li>
          ))}
        </ul>
      )}
    </li>
  )
}

export default SidebarNavItem
