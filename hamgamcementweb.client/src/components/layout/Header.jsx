import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../../context/AuthContext'
import { useTheme } from '../../context/ThemeContext'
import { fetchDashboardNotifications } from '../../services/dashboardApi'
import {
  clearPushToastBatch,
  getBrowserNotificationPermission,
  getUnseenNotifications,
  markNotificationsSeen,
  preparePushToasts,
  requestBrowserNotificationPermission,
  showBrowserNotifications,
} from '../../lib/pushNotifications'
import Icon from '../common/Icon'
import PushToastStack from '../common/PushToastStack'

function Header({ isSidebarExpanded, onSidebarToggle }) {
  const { theme, toggleTheme } = useTheme()
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const [notifications, setNotifications] = useState([])
  const [notifLoading, setNotifLoading] = useState(true)
  const [pushToasts, setPushToasts] = useState([])
  const [browserPerm, setBrowserPerm] = useState(getBrowserNotificationPermission())

  useEffect(() => {
    let cancelled = false

    async function load() {
      setNotifLoading(true)
      try {
        const data = await fetchDashboardNotifications()
        if (cancelled) return
        const items = Array.isArray(data?.items) ? data.items : []
        setNotifications(items)

        const toasts = preparePushToasts(items)
        if (toasts.length) {
          setPushToasts(toasts)
          showBrowserNotifications(toasts)
        }
      } catch {
        if (!cancelled) setNotifications([])
      } finally {
        if (!cancelled) setNotifLoading(false)
      }
    }

    load()
    return () => {
      cancelled = true
    }
  }, [])

  const handleLogout = async () => {
    await logout()
    navigate('/login', { replace: true })
  }

  const handleBellClick = async () => {
    if (browserPerm === 'default') {
      const next = await requestBrowserNotificationPermission()
      setBrowserPerm(next)
      if (next === 'granted' && notifications.length) {
        const unseen = getUnseenNotifications(notifications)
        // اگر قبلاً در همین نشست دیده شده‌اند، یک خلاصهٔ کلی نشان بده
        if (unseen.length) {
          showBrowserNotifications(unseen)
          markNotificationsSeen(unseen)
        } else {
          showBrowserNotifications(
            notifications.slice(0, 3).map((item, index) => ({
              ...item,
              fingerprint: `manual|${item.type}|${item.productId ?? item.warehouseId ?? index}`,
            })),
          )
        }
      }
    }
  }

  const hasNotifications = !notifLoading && notifications.length > 0

  return (
    <>
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

          <div className="dropdown">
            <button
              type="button"
              className="btn btn-header-icon"
              data-bs-toggle="dropdown"
              aria-expanded="false"
              aria-label="اعلان‌ها"
              onClick={handleBellClick}
            >
              <Icon name="bell" />
              {hasNotifications && <span className="notification-dot" />}
            </button>
            <div className="dropdown-menu dropdown-menu-end header-notif-dropdown">
              <div className="header-notif-head">
                <strong>اعلان‌ها</strong>
                {hasNotifications && (
                  <span className="badge badge-status">{notifications.length}</span>
                )}
              </div>
              {browserPerm === 'default' && (
                <div className="header-notif-hint">
                  برای دریافت پوش نوتیفیکیشن سیستم، روی زنگوله کلیک کنید و اجازه دهید.
                </div>
              )}
              {notifLoading && (
                <div className="header-notif-empty">در حال بارگذاری…</div>
              )}
              {!notifLoading && notifications.length === 0 && (
                <div className="header-notif-empty">اعلان فعالی وجود ندارد.</div>
              )}
              {!notifLoading &&
                notifications.slice(0, 8).map((item, index) => (
                  <Link
                    key={`${item.type}-${item.productId ?? item.warehouseId ?? index}`}
                    to={item.href || '/'}
                    className="dropdown-item header-notif-item"
                  >
                    <span className={`header-notif-severity is-${item.severity || 'info'}`} />
                    <span>
                      <span className="header-notif-title">{item.title}</span>
                      <span className="header-notif-message">{item.message}</span>
                    </span>
                  </Link>
                ))}
              {!notifLoading && notifications.length > 0 && (
                <Link to="/" className="dropdown-item header-notif-footer text-center">
                  مشاهده داشبورد
                </Link>
              )}
            </div>
          </div>

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

      <PushToastStack
        items={pushToasts}
        onDismissAll={() => {
          setPushToasts([])
          clearPushToastBatch()
        }}
      />
    </>
  )
}

export default Header
