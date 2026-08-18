import { useEffect, useState } from 'react'
import Icon from '../../components/common/Icon'
import DashboardNotificationsPanel from '../../components/dashboard/DashboardNotificationsPanel'
import DashboardRecentOperations from '../../components/dashboard/DashboardRecentOperations'
import PerformanceAnalysisChart from '../../components/dashboard/PerformanceAnalysisChart'
import { formatAmount } from '../../lib/dataTableOptions'
import { fetchDashboardSummary } from '../../services/dashboardApi'

const STAT_CARD_DEFS = [
  {
    key: 'todayTrips',
    title: 'سفرهای امروز',
    unit: 'سفر',
    icon: 'truck',
  },
  {
    key: 'monthTrips',
    title: 'سفرهای ماه جاری',
    unit: 'سفر',
    icon: 'route',
  },
  {
    key: 'todayTripRevenue',
    title: 'درآمد حمل امروز',
    unit: '',
    icon: 'sales-check',
  },
  {
    key: 'monthTripRevenue',
    title: 'درآمد حمل ماه جاری',
    unit: '',
    icon: 'chart-up',
  },
  {
    key: 'activeVehicles',
    title: 'وسایل فعال',
    unit: 'دستگاه',
    icon: 'production',
  },
]

function formatStatValue(value, card) {
  if (value === null || value === undefined || value === '') return '—'
  const num = Number(value)
  if (!Number.isFinite(num)) return '—'
  if (card.key === 'todayTrips' || card.key === 'monthTrips' || card.key === 'activeVehicles') {
    return num.toLocaleString('fa-IR')
  }
  return formatAmount(num)
}

function DashboardPage() {
  const [summary, setSummary] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let cancelled = false

    async function load() {
      setLoading(true)
      setError('')
      try {
        const data = await fetchDashboardSummary()
        if (!cancelled) setSummary(data)
      } catch (err) {
        if (!cancelled) {
          setSummary(null)
          setError(err.message || 'بارگذاری خلاصه داشبورد با خطا مواجه شد.')
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
    <div className="dashboard-page">
      <section className="mb-4">
        <div className="page-welcome card border-0">
          <div className="card-body p-4">
            <h2 className="welcome-title mb-2">خوش آمدید</h2>
            <p className="welcome-text mb-0">
              نمای کلی عملکرد ناوگان، سفرها، درآمد و هزینه‌های حمل و وضعیت مالی شرکت.
            </p>
          </div>
        </div>
      </section>

      <section className="mb-4">
        {error ? (
          <div className="alert alert-danger mb-3" role="alert">
            {error}
          </div>
        ) : null}
        <div className="row g-3">
          {STAT_CARD_DEFS.map((card) => (
            <div key={card.key} className="col-12 col-sm-6 col-xl">
              <div className="stat-card card h-100 border-0">
                <div className="card-body p-3 p-md-4">
                  <div className="d-flex align-items-start justify-content-between mb-3">
                    <div className="stat-icon">
                      <Icon name={card.icon} />
                    </div>
                  </div>
                  <p className="stat-label mb-1">{card.title}</p>
                  <div className="d-flex align-items-baseline gap-2 flex-wrap">
                    <span className="stat-value">
                      {loading ? '…' : formatStatValue(summary?.[card.key], card)}
                    </span>
                    {card.unit ? <span className="stat-unit">{card.unit}</span> : null}
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      </section>

      <section className="mb-4">
        <div className="row g-3">
          <div className="col-12 col-lg-8">
            <PerformanceAnalysisChart />
          </div>

          <div className="col-12 col-lg-4">
            <DashboardNotificationsPanel />
          </div>
        </div>
      </section>

      <section>
        <DashboardRecentOperations />
      </section>
    </div>
  )
}

export default DashboardPage
