import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import Icon from '../common/Icon'
import { fetchDashboardNotifications } from '../../services/dashboardApi'

function severityIcon(type) {
  if (type === 'product_shortage') return 'triangle-exclamation'
  if (type === 'warehouse_full') return 'warehouse'
  if (type === 'warehouse_low') return 'boxes-stacked'
  return 'bell'
}

function DashboardNotificationsPanel() {
  const [items, setItems] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let cancelled = false

    async function load() {
      setLoading(true)
      setError('')
      try {
        const data = await fetchDashboardNotifications()
        if (!cancelled) setItems(Array.isArray(data?.items) ? data.items : [])
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
  }, [])

  return (
    <div className="content-card card border-0 h-100">
      <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between gap-2">
        <h3 className="card-title mb-0">اعلان‌ها</h3>
        {!loading && !error && items.length > 0 && (
          <span className="badge badge-status">{items.length}</span>
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
            {items.map((item, index) => (
              <li key={`${item.type}-${item.productId ?? item.warehouseId ?? index}`} className="activity-item">
                <div className={`activity-dot is-${item.severity || 'info'}`} />
                <div className="flex-grow-1">
                  <p className="activity-title mb-1 d-flex align-items-start gap-2">
                    <Icon name={severityIcon(item.type)} className="activity-type-icon mt-1" />
                    <span>{item.title}</span>
                  </p>
                  <span className="activity-time d-block mb-1">{item.message}</span>
                  {item.href && (
                    <Link to={item.href} className="activity-link">
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
