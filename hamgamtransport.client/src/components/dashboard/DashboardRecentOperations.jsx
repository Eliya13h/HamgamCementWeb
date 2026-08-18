import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { formatAmount } from '../../lib/dataTableOptions'
import { formatJalaliDate } from '../../lib/afghanSolarCalendar'
import { fetchDashboardRecentOperations } from '../../services/dashboardApi'

function typeBadgeClass(type) {
  if (type === 'trip') return 'is-production'
  if (type === 'revenue') return 'is-sale'
  if (type === 'expense') return 'is-purchase'
  return ''
}

function DashboardRecentOperations() {
  const [rows, setRows] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let cancelled = false

    async function load() {
      setLoading(true)
      setError('')
      try {
        const data = await fetchDashboardRecentOperations(15)
        if (!cancelled) setRows(Array.isArray(data) ? data : [])
      } catch (err) {
        if (!cancelled) {
          setRows([])
          setError(err.message || 'بارگذاری آخرین عملیات با خطا مواجه شد.')
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
    <div className="content-card card border-0">
      <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between flex-wrap gap-2">
        <div>
          <h3 className="card-title mb-1">آخرین عملیات</h3>
          <p className="silo-section-subtitle mb-0">سفرها، عواید و مصارف به‌ترتیب تاریخ</p>
        </div>
        <div className="d-flex flex-wrap gap-2">
          <Link to="/transport/trips" className="btn btn-sm btn-outline-accent">
            سفرها
          </Link>
          <Link to="/accounting/revenues" className="btn btn-sm btn-outline-accent">
            عواید
          </Link>
          <Link to="/accounting/expenses" className="btn btn-sm btn-outline-accent">
            مصارف
          </Link>
        </div>
      </div>

      <div className="card-body p-4">
        {loading && (
          <p className="placeholder-text mb-0">در حال بارگذاری عملیات…</p>
        )}

        {!loading && error && <p className="text-danger mb-0">{error}</p>}

        {!loading && !error && rows.length === 0 && (
          <p className="placeholder-text mb-0">هنوز عملیاتی ثبت نشده است.</p>
        )}

        {!loading && !error && rows.length > 0 && (
          <div className="table-responsive">
            <table className="table table-dark table-hover dashboard-table mb-0">
              <thead>
                <tr>
                  <th scope="col">#</th>
                  <th scope="col">نوع</th>
                  <th scope="col">عنوان</th>
                  <th scope="col">طرف حساب</th>
                  <th scope="col">مبلغ پایه</th>
                  <th scope="col">وضعیت</th>
                  <th scope="col">تاریخ</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((row, index) => (
                  <tr key={`${row.type}-${row.entityId}`}>
                    <td>{index + 1}</td>
                    <td>
                      <span className={`badge badge-op ${typeBadgeClass(row.type)}`}>
                        {row.typeLabel}
                      </span>
                    </td>
                    <td>
                      {row.href ? (
                        <Link to={row.href} className="dashboard-row-link">
                          {row.title}
                        </Link>
                      ) : (
                        row.title
                      )}
                    </td>
                    <td>{row.partyName || '—'}</td>
                    <td>{formatAmount(row.amountInBase)}</td>
                    <td>
                      <span className="badge badge-status">{row.statusLabel}</span>
                    </td>
                    <td>{row.dateLabel || formatJalaliDate(row.operationDate)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}

export default DashboardRecentOperations
