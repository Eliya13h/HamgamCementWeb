import { useTheme } from '../../context/ThemeContext'
import Icon from '../common/Icon'

function Header({ isSidebarExpanded, onSidebarToggle }) {
  const { theme, toggleTheme } = useTheme()

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

        <button type="button" className="header-user btn p-0 border-0">
          <span className="user-avatar">
            <Icon name="user" />
          </span>
          <span className="user-name d-none d-sm-inline">امیرعلی</span>
          <Icon name="chevron-down" className="user-chevron d-none d-sm-inline" />
        </button>
      </div>
    </header>
  )
}

export default Header
