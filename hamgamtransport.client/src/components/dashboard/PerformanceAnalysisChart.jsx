import { useEffect, useMemo, useState } from 'react'
import {
  Area,
  CartesianGrid,
  ComposedChart,
  Legend,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { formatAmount } from '../../lib/dataTableOptions'
import { fetchDashboardPerformance } from '../../services/dashboardApi'

const RANGE_OPTIONS = [
  { value: 1, label: '۱ ماه' },
  { value: 3, label: '۳ ماه' },
  { value: 6, label: '۶ ماه' },
  { value: 12, label: '۱۲ ماه' },
]

const SERIES = [
  { key: 'tripRevenue', label: 'درآمد حمل', color: '#3fb950', totalKey: 'tripRevenue' },
  { key: 'tripExpense', label: 'هزینه سفر', color: '#f85149', totalKey: 'tripExpense' },
  { key: 'revenue', label: 'سایر عواید', color: '#58a6ff', totalKey: 'revenue' },
  { key: 'expense', label: 'سایر مصارف', color: '#f0883e', totalKey: 'expense' },
]

function readCssVar(name, fallback) {
  if (typeof window === 'undefined') return fallback
  const value = getComputedStyle(document.documentElement).getPropertyValue(name).trim()
  return value || fallback
}

function formatAxisValue(value) {
  const num = Number(value)
  if (!Number.isFinite(num)) return '0'
  const abs = Math.abs(num)
  if (abs >= 1_000_000_000) return `${(num / 1_000_000_000).toFixed(1)}B`
  if (abs >= 1_000_000) return `${(num / 1_000_000).toFixed(1)}M`
  if (abs >= 1_000) return `${(num / 1_000).toFixed(1)}K`
  return formatAmount(num)
}

function ChartTooltip({ active, payload, label, textColor, mutedColor, surface, border }) {
  if (!active || !payload?.length) return null

  return (
    <div
      className="performance-chart-tooltip"
      style={{
        background: surface,
        borderColor: border,
        color: textColor,
      }}
    >
      <div className="performance-chart-tooltip-title" style={{ color: textColor }}>
        {label}
      </div>
      {SERIES.map((series) => {
        const item = payload.find((entry) => entry.dataKey === series.key)
        if (!item) return null
        return (
          <div key={series.key} className="performance-chart-tooltip-row">
            <span className="performance-chart-tooltip-swatch" style={{ background: series.color }} />
            <span style={{ color: mutedColor }}>{series.label}</span>
            <strong style={{ color: textColor }}>{formatAmount(item.value)}</strong>
          </div>
        )
      })}
    </div>
  )
}

function PerformanceAnalysisChart() {
  const [months, setMonths] = useState(1)
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [themeTick, setThemeTick] = useState(0)

  useEffect(() => {
    const root = document.documentElement
    const observer = new MutationObserver(() => setThemeTick((value) => value + 1))
    observer.observe(root, { attributes: true, attributeFilter: ['data-bs-theme'] })
    return () => observer.disconnect()
  }, [])

  useEffect(() => {
    let cancelled = false

    async function load() {
      setLoading(true)
      setError('')
      try {
        const result = await fetchDashboardPerformance(months)
        if (!cancelled) setData(result)
      } catch (err) {
        if (!cancelled) {
          setData(null)
          setError(err.message || 'بارگذاری نمودار با خطا مواجه شد.')
        }
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    load()
    return () => {
      cancelled = true
    }
  }, [months])

  const theme = useMemo(() => {
    void themeTick
    return {
      text: readCssVar('--hc-text', '#e6edf3'),
      muted: readCssVar('--hc-text-muted', '#8b949e'),
      border: readCssVar('--hc-border', '#30363d'),
      surface: readCssVar('--hc-bg-elevated', '#161b22'),
      grid: readCssVar('--hc-border-subtle', '#21262d'),
    }
  }, [themeTick])

  const points = data?.points ?? []
  const totals = data?.totals ?? { tripRevenue: 0, tripExpense: 0, revenue: 0, expense: 0 }
  const hasValues = points.some(
    (point) =>
      Number(point.tripRevenue) ||
      Number(point.tripExpense) ||
      Number(point.revenue) ||
      Number(point.expense),
  )

  return (
    <div className="content-card card border-0 h-100">
      <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0">
        <div className="d-flex flex-wrap align-items-start justify-content-between gap-3">
          <div>
            <h3 className="card-title mb-1">تحلیل عملکرد مالی</h3>
            <p className="performance-chart-subtitle mb-0">
              مقایسه ماهانه درآمد/هزینه حمل و سایر عواید و مصارف
              {data?.from && data?.to ? ` · ${data.from} تا ${data.to}` : ''}
            </p>
          </div>
          <div className="performance-range-toggle" role="group" aria-label="بازه زمانی نمودار">
            {RANGE_OPTIONS.map((option) => (
              <button
                key={option.value}
                type="button"
                className={`performance-range-btn${months === option.value ? ' is-active' : ''}`}
                onClick={() => setMonths(option.value)}
              >
                {option.label}
              </button>
            ))}
          </div>
        </div>
      </div>

      <div className="card-body p-4">
        <div className="performance-summary-grid mb-3">
          {SERIES.map((series) => (
            <div key={series.key} className="performance-summary-item">
              <span className="performance-summary-dot" style={{ background: series.color }} />
              <div>
                <p className="performance-summary-label mb-0">{series.label}</p>
                <strong className="performance-summary-value">
                  {formatAmount(totals[series.totalKey] ?? 0)}
                </strong>
              </div>
            </div>
          ))}
        </div>

        {loading && (
          <div className="performance-chart-empty">
            <p className="placeholder-text mb-0">در حال آماده‌سازی نمودار…</p>
          </div>
        )}

        {!loading && error && (
          <div className="performance-chart-empty">
            <p className="text-danger mb-0">{error}</p>
          </div>
        )}

        {!loading && !error && !hasValues && (
          <div className="performance-chart-empty">
            <p className="placeholder-text mb-0">در این بازه هنوز داده‌ای برای نمایش وجود ندارد.</p>
          </div>
        )}

        {!loading && !error && hasValues && (
          <div className="performance-chart-canvas" dir="ltr">
            <ResponsiveContainer width="100%" height="100%">
              <ComposedChart data={points} margin={{ top: 12, right: 8, left: 0, bottom: 0 }}>
                <defs>
                  {SERIES.map((series) => (
                    <linearGradient
                      key={series.key}
                      id={`perf-fill-${series.key}`}
                      x1="0"
                      y1="0"
                      x2="0"
                      y2="1"
                    >
                      <stop offset="0%" stopColor={series.color} stopOpacity={0.28} />
                      <stop offset="100%" stopColor={series.color} stopOpacity={0.02} />
                    </linearGradient>
                  ))}
                </defs>

                <CartesianGrid stroke={theme.grid} strokeDasharray="4 6" vertical={false} />
                <XAxis
                  dataKey="label"
                  tick={{ fill: theme.muted, fontSize: 11 }}
                  axisLine={{ stroke: theme.border }}
                  tickLine={false}
                  interval="preserveStartEnd"
                  minTickGap={18}
                />
                <YAxis
                  tick={{ fill: theme.muted, fontSize: 11 }}
                  axisLine={false}
                  tickLine={false}
                  width={52}
                  tickFormatter={formatAxisValue}
                />
                <Tooltip
                  content={
                    <ChartTooltip
                      textColor={theme.text}
                      mutedColor={theme.muted}
                      surface={theme.surface}
                      border={theme.border}
                    />
                  }
                  cursor={{ stroke: theme.border, strokeDasharray: '4 4' }}
                />
                <Legend
                  verticalAlign="top"
                  align="left"
                  iconType="circle"
                  wrapperStyle={{ paddingBottom: 12, color: theme.muted, fontSize: 12 }}
                  formatter={(value) => (
                    <span style={{ color: theme.muted }}>{value}</span>
                  )}
                />

                {SERIES.map((series) => (
                  <Area
                    key={series.key}
                    type="monotone"
                    dataKey={series.key}
                    name={series.label}
                    stroke={series.color}
                    strokeWidth={2.4}
                    fill={`url(#perf-fill-${series.key})`}
                    fillOpacity={1}
                    dot={false}
                    activeDot={{ r: 5, strokeWidth: 0, fill: series.color }}
                    isAnimationActive
                    animationDuration={700}
                  />
                ))}
              </ComposedChart>
            </ResponsiveContainer>
          </div>
        )}
      </div>
    </div>
  )
}

export default PerformanceAnalysisChart
