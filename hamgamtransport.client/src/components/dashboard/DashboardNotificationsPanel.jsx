import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import Icon from '../common/Icon'
import { useAuth } from '../../context/AuthContext'
import { fetchDashboardNotifications } from '../../services/dashboardApi'
import {
  getUnreadNotifications,
  markNotificationsRead,
  syncNotificationState,
} from '../../lib/pushNotifications'

function severityIcon(type) {
  if (type === 'trip_pending') return 'route'
  if (type === 'trip_unposted') return 'triangle-exclamation'
  if (type === 'trip_awaiting_settlement') return 'cash'
  return 'bell'
}

function DashboardNotificationsPanel() {
  const { user } = useAuth()
  const userId = user?.userId
  const [items, setItems] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    if (userId == null) return undefined

    let cancelled = false

    async function load() {
      setLoading(true)
      setError('')
      try {
        const data = await fetchDashboardNotifications()
        if (cancelled) return
        const synced = syncNotificationState(
          Array.isArray(data?.items) ? data.items : [],
          userId,
        )
        setItems(synced)
      } catch (err) {
        if (!cancelled) {
          setItems([])
          setError(err.message || 'بارگذاری اعلان‌ها با خطا مواجه شد.')
        }
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    load()
    return () => {
      cancelled = true
    }
  }, [userId])

  const unreadCount = getUnreadNotifications(items).length

  const handleItemOpen = (item) => {
    markNotificationsRead([item], userId)
    setItems((prev) =>
      prev.map((row) =>
        row.fingerprint === item.fingerprint ? { ...row, isRead: true } : row,
      ),
    )
  }

  return (
    <div className="content-card card border-0 h-100">
      <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between gap-2">
        <h3 className="card-title mb-0">اعلان‌ها</h3>
        {!loading && !error && items.length > 0 && (
          <span className="badge badge-status">
            {unreadCount > 0 ? `${unreadCount} جدید` : items.length}
          </span>
        )}
      </div>

      <div className="card-body p-4">
        {loading && (
          <p className="placeholder-text mb-0">در حال بارگذاری اعلان‌ها…</p>
        )}

        {!loading && error && <p className="text-danger mb-0">{error}</p>}

        {!loading && !error && items.length === 0 && (
          <p className="placeholder-text mb-0">اعلان فعالی وجود ندارد.</p>
        )}

        {!loading && !error && items.length > 0 && (
          <ul className="activity-list list-unstyled mb-0">
            {items.map((item) => (
              <li
                key={item.key || item.fingerprint}
                className={`activity-item${item.isRead ? '' : ' is-unread'}`}
              >
                <div className={`activity-dot is-${item.severity || 'info'}`} />
                <div className="flex-grow-1">
                  <p className="activity-title mb-1 d-flex align-items-start gap-2">
                    <Icon name={severityIcon(item.type)} className="activity-type-icon mt-1" />
                    <span className="d-flex align-items-center gap-2 flex-wrap">
                      <span>{item.title}</span>
                      {!item.isRead && <span className="header-notif-new">جدید</span>}
                    </span>
                  </p>
                  <span className="activity-time d-block mb-1">{item.message}</span>
                  {item.href && (
                    <Link
                      to={item.href}
                      className="activity-link"
                      onClick={() => handleItemOpen(item)}
                    >
                      مشاهده
                    </Link>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  )
}

export default DashboardNotificationsPanel
