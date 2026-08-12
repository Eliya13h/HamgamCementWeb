import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../../context/AuthContext'
import { useTheme } from '../../context/ThemeContext'
import { fetchDashboardNotifications } from '../../services/dashboardApi'
import {
  clearPushToastBatch,
  getBrowserNotificationPermission,
  getUnreadNotifications,
  markNotificationsRead,
  preparePushToasts,
  requestBrowserNotificationPermission,
  showBrowserNotifications,
  syncNotificationState,
} from '../../lib/pushNotifications'
import Icon from '../common/Icon'
import PushToastStack from '../common/PushToastStack'

const POLL_INTERVAL_MS = 60_000

function Header({ isSidebarExpanded, onSidebarToggle }) {
  const { theme, toggleTheme } = useTheme()
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const userId = user?.userId
  const [notifications, setNotifications] = useState([])
  const [notifLoading, setNotifLoading] = useState(true)
  const [pushToasts, setPushToasts] = useState([])
  const [browserPerm, setBrowserPerm] = useState(getBrowserNotificationPermission())
  const [openMenu, setOpenMenu] = useState(null)
  // اثر نخوانده هنگام باز بودن پنل تا کاربر «جدید» را ببیند؛ badge همان لحظه صفر می‌شود
  const [panelUnreadKeys, setPanelUnreadKeys] = useState(() => new Set())
  const notifMenuRef = useRef(null)
  const userMenuRef = useRef(null)

  const applyNotifications = useCallback(
    (rawItems, { pushNew = true } = {}) => {
      const synced = syncNotificationState(rawItems, userId)
      setNotifications(synced)

      if (!pushNew) return

      const toasts = preparePushToasts(synced, userId)
      if (toasts.length) {
        setPushToasts(toasts)
        showBrowserNotifications(toasts)
      }
    },
    [userId],
  )

  const loadNotifications = useCallback(
    async ({ silent = false, pushNew = true } = {}) => {
      if (!silent) setNotifLoading(true)
      try {
        const data = await fetchDashboardNotifications()
        const items = Array.isArray(data?.items) ? data.items : []
        applyNotifications(items, { pushNew })
      } catch {
        if (!silent) setNotifications([])
      } finally {
        if (!silent) setNotifLoading(false)
      }
    },
    [applyNotifications],
  )

  // صبر تا userId مشخص شود تا وضعیت خوانده‌شده بین anon/user قاطی نشود
  useEffect(() => {
    if (userId == null) return
    loadNotifications({ pushNew: true })
  }, [userId, loadNotifications])

  // پولینگ سبک برای حس پوش: فقط اعلان‌های جدید توست/نوتیف می‌شوند
  useEffect(() => {
    if (userId == null) return undefined

    const timer = window.setInterval(() => {
      if (document.visibilityState !== 'visible') return
      loadNotifications({ silent: true, pushNew: true })
    }, POLL_INTERVAL_MS)

    const onVisible = () => {
      if (document.visibilityState === 'visible') {
        loadNotifications({ silent: true, pushNew: true })
      }
    }
    document.addEventListener('visibilitychange', onVisible)

    return () => {
      window.clearInterval(timer)
      document.removeEventListener('visibilitychange', onVisible)
    }
  }, [userId, loadNotifications])

  useEffect(() => {
    setOpenMenu(null)
    setPanelUnreadKeys(new Set())
  }, [location.pathname])

  useEffect(() => {
    if (!openMenu) return

    const handlePointerDown = (event) => {
      const root = openMenu === 'notif' ? notifMenuRef.current : userMenuRef.current
      if (root && !root.contains(event.target)) {
        setOpenMenu(null)
        setPanelUnreadKeys(new Set())
      }
    }

    const handleKeyDown = (event) => {
      if (event.key === 'Escape') {
        setOpenMenu(null)
        setPanelUnreadKeys(new Set())
      }
    }

    document.addEventListener('mousedown', handlePointerDown)
    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.removeEventListener('mousedown', handlePointerDown)
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [openMenu])

  const markAllReadInUi = useCallback(() => {
    if (!notifications.length) return
    markNotificationsRead(notifications, userId)
    setNotifications((prev) => prev.map((item) => ({ ...item, isRead: true })))
  }, [notifications, userId])

  const handleLogout = async () => {
    setOpenMenu(null)
    await logout()
    navigate('/login', { replace: true })
  }

  const handleBellClick = async () => {
    const willOpen = openMenu !== 'notif'

    if (willOpen) {
      const unreadKeys = new Set(
        getUnreadNotifications(notifications).map((item) => item.fingerprint),
      )
      setPanelUnreadKeys(unreadKeys)
      markAllReadInUi()
      setOpenMenu('notif')
    } else {
      setOpenMenu(null)
      setPanelUnreadKeys(new Set())
    }

    if (browserPerm === 'default') {
      const next = await requestBrowserNotificationPermission()
      setBrowserPerm(next)
    }
  }

  const handleNotifItemClick = (item) => {
    markNotificationsRead([item], userId)
    setNotifications((prev) =>
      prev.map((row) =>
        row.fingerprint === item.fingerprint ? { ...row, isRead: true } : row,
      ),
    )
    setPanelUnreadKeys((prev) => {
      const next = new Set(prev)
      next.delete(item.fingerprint)
      return next
    })
    setOpenMenu(null)
  }

  const unreadCount = getUnreadNotifications(notifications).length
  const hasNotifications = !notifLoading && notifications.length > 0
  const notifOpen = openMenu === 'notif'
  const userOpen = openMenu === 'user'

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

          <div className="dropdown" ref={notifMenuRef}>
            <button
              type="button"
              className="btn btn-header-icon"
              aria-expanded={notifOpen}
              aria-haspopup="true"
              aria-label={
                unreadCount > 0 ? `اعلان‌ها — ${unreadCount} خوانده‌نشده` : 'اعلان‌ها'
              }
              onClick={handleBellClick}
            >
              <Icon name="bell" />
              {unreadCount > 0 && <span className="notification-dot" />}
            </button>
            <div
              className={`dropdown-menu dropdown-menu-end header-notif-dropdown${notifOpen ? ' show' : ''}`}
            >
              <div className="header-notif-head">
                <strong>اعلان‌ها</strong>
                {hasNotifications && (
                  <span className="badge badge-status">
                    {unreadCount > 0 ? `${unreadCount} جدید` : notifications.length}
                  </span>
                )}
              </div>
              {browserPerm === 'default' && (
                <div className="header-notif-hint">
                  برای دریافت پوش نوتیفیکیشن مرورگر، اجازه دهید تا فقط اعلان‌های جدید به شما برسد.
                </div>
              )}
              {notifLoading && (
                <div className="header-notif-empty">در حال بارگذاری…</div>
              )}
              {!notifLoading && notifications.length === 0 && (
                <div className="header-notif-empty">اعلان فعالی وجود ندارد.</div>
              )}
              {!notifLoading &&
                notifications.slice(0, 8).map((item) => {
                  const showUnread = panelUnreadKeys.has(item.fingerprint) || !item.isRead
                  return (
                    <Link
                      key={item.key || item.fingerprint}
                      to={item.href || '/'}
                      className={`dropdown-item header-notif-item${showUnread ? ' is-unread' : ''}`}
                      onClick={() => handleNotifItemClick(item)}
                    >
                      <span className={`header-notif-severity is-${item.severity || 'info'}`} />
                      <span className="header-notif-body">
                        <span className="header-notif-title-row">
                          <span className="header-notif-title">{item.title}</span>
                          {showUnread && <span className="header-notif-new">جدید</span>}
                        </span>
                        <span className="header-notif-message">{item.message}</span>
                      </span>
                    </Link>
                  )
                })}
              {!notifLoading && notifications.length > 0 && (
                <Link
                  to="/"
                  className="dropdown-item header-notif-footer text-center"
                  onClick={() => setOpenMenu(null)}
                >
                  مشاهده داشبورد
                </Link>
              )}
            </div>
          </div>

          <div className="dropdown" ref={userMenuRef}>
            <button
              type="button"
              className="header-user btn p-0 border-0"
              aria-expanded={userOpen}
              aria-haspopup="true"
              aria-label="منوی کاربر"
              onClick={() => setOpenMenu((prev) => (prev === 'user' ? null : 'user'))}
            >
              <span className="user-avatar">
                <Icon name="user" />
              </span>
              <span className="user-name d-none d-sm-inline">{user?.fullName ?? 'کاربر'}</span>
              <Icon
                name="chevron-down"
                className={`user-chevron d-none d-sm-inline${userOpen ? ' is-open' : ''}`}
              />
            </button>
            <ul className={`dropdown-menu dropdown-menu-end${userOpen ? ' show' : ''}`}>
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
