import { useNavigate } from 'react-router-dom'
import { useAuth } from '../../context/AuthContext'
import { useTheme } from '../../context/ThemeContext'
import Icon from '../common/Icon'

function Header({ isSidebarExpanded, onSidebarToggle }) {
  const { theme, toggleTheme } = useTheme()
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const handleLogout = async () => {
    await logout()
    navigate('/login', { replace: true })
  }

  return (
    <header className="dashboard-header d-flex align-items-center gap-3">
      <button
        type="button"
        className="btn btn-header-icon btn-sidebar-trigger flex-shrink-0"
        onClick={onSidebarToggle}
        aria-label={isSidebarExpanded ? 'بستن منو' : 'باز کردن منو'}
        aria-expanded={isSidebarExpanded}
      >
        <Icon name={isSidebarExpanded ? 'sidebar-close' : 'sidebar-open'} />
      </button>

      <div className="header-search flex-grow-1">
        <Icon name="search" className="search-icon" />
        <input
          type="search"
          className="form-control"
          placeholder="جستجو"
          aria-label="جستجو"
        />
      </div>

      <div className="header-actions d-flex align-items-center gap-2 flex-shrink-0">
        <button
          type="button"
          className="btn btn-header-icon"
          onClick={toggleTheme}
          aria-label={theme === 'dark' ? 'حالت روشن' : 'حالت تاریک'}
        >
          <Icon name={theme === 'dark' ? 'sun' : 'moon'} />
        </button>

        <button type="button" className="btn btn-header-icon" aria-label="اعلان‌ها">
          <Icon name="bell" />
          <span className="notification-dot" />
        </button>

        <div className="dropdown">
          <button
            type="button"
            className="header-user btn p-0 border-0 dropdown-toggle"
            data-bs-toggle="dropdown"
            aria-expanded="false"
          >
            <span className="user-avatar">
              <Icon name="user" />
            </span>
            <span className="user-name d-none d-sm-inline">{user?.fullName ?? 'کاربر'}</span>
            <Icon name="chevron-down" className="user-chevron d-none d-sm-inline" />
          </button>
          <ul className="dropdown-menu dropdown-menu-end">
            <li>
              <span className="dropdown-item-text small text-muted">
                {user?.roleName ?? '—'}
              </span>
            </li>
            <li>
              <hr className="dropdown-divider" />
            </li>
            <li>
              <button type="button" className="dropdown-item" onClick={handleLogout}>
                <Icon name="sign-out" className="ms-2" />
                خروج از حساب
              </button>
            </li>
          </ul>
        </div>
      </div>
    </header>
  )
}

export default Header
