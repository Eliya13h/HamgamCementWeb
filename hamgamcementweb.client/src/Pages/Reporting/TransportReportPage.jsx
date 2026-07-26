import { useCallback, useEffect, useState } from 'react'
import AmountDisplay from '../../components/common/AmountDisplay'
import JalaliDateField from '../../components/common/JalaliDateField'
import { formatAmount } from '../Transport/CrudTablePage'

async function fetchTransportSummary(fromDate, toDate) {
  const params = new URLSearchParams()
  if (fromDate) params.set('fromDate', fromDate)
  if (toDate) params.set('toDate', toDate)
  const qs = params.toString()
  const response = await fetch(`/api/reports/transport/summary${qs ? `?${qs}` : ''}`, {
    credentials: 'include',
  })
  const data = await response.json().catch(() => null)
  if (!response.ok) {
    throw new Error(data?.message ?? 'بارگذاری گزارش حمل ناموفق بود.')
  }
  return data
}

function TransportReportPage() {
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [summary, setSummary] = useState(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const data = await fetchTransportSummary(fromDate || null, toDate || null)
      setSummary(data)
    } catch (e) {
      setSummary(null)
      setError(e.message)
    } finally {
      setLoading(false)
    }
  }, [fromDate, toDate])

  useEffect(() => {
    load()
  }, [load])

  return (
    <div className="container-fluid py-3">
      <div className="d-flex flex-wrap align-items-end justify-content-between gap-3 mb-3">
        <div>
          <h4 className="mb-1">گزارش ترانسپورت</h4>
          <p className="text-muted mb-0 small">خلاصه تن، کرایه، خودی/کرایه‌ای، تعمیرات و استهلاک</p>
        </div>
        <div className="d-flex flex-wrap gap-2 align-items-end">
          <div>
            <label className="form-label small mb-1">از تاریخ</label>
            <JalaliDateField value={fromDate} onChange={setFromDate} />
          </div>
          <div>
            <label className="form-label small mb-1">تا تاریخ</label>
            <JalaliDateField value={toDate} onChange={setToDate} />
          </div>
          <button type="button" className="btn btn-primary" onClick={load} disabled={loading}>
            {loading ? '...' : 'اعمال'}
          </button>
        </div>
      </div>

      {error && <div className="alert alert-danger">{error}</div>}

      {summary && (
        <>
          <div className="row g-3 mb-4">
            <div className="col-md-3">
              <div className="border rounded p-3 h-100">
                <div className="text-muted small">تعداد سفر</div>
                <div className="fs-4">{formatAmount(summary.totalTrips)}</div>
              </div>
            </div>
            <div className="col-md-3">
              <div className="border rounded p-3 h-100">
                <div className="text-muted small">جمع وزن (تن)</div>
                <div className="fs-4">{formatAmount(summary.totalWeightTon)}</div>
              </div>
            </div>
            <div className="col-md-3">
              <div className="border rounded p-3 h-100">
                <div className="text-muted small">درآمد سفر / فروش</div>
                <div className="fs-4">
                  <AmountDisplay value={summary.totalTripRevenue} />
                </div>
              </div>
            </div>
            <div className="col-md-3">
              <div className="border rounded p-3 h-100">
                <div className="text-muted small">خودی / کرایه‌ای</div>
                <div className="fs-5">
                  {formatAmount(summary.ownFleetTrips)} / {formatAmount(summary.hiredTrips)}
                </div>
              </div>
            </div>
          </div>

          <div className="row g-4">
            <div className="col-lg-6">
              <h6>بر اساس هدف سفر</h6>
              <div className="table-responsive">
                <table className="table table-sm align-middle">
                  <thead>
                    <tr>
                      <th>هدف</th>
                      <th className="text-center">سفر</th>
                      <th className="text-center">تن</th>
                      <th className="text-center">درآمد</th>
                    </tr>
                  </thead>
                  <tbody>
                    {(summary.byPurpose ?? []).map((row) => (
                      <tr key={row.tripPurpose}>
                        <td>{row.tripPurposeName}</td>
                        <td className="text-center">{formatAmount(row.tripCount)}</td>
                        <td className="text-center">{formatAmount(row.totalWeightTon)}</td>
                        <td className="text-center">
                          <AmountDisplay value={row.totalRevenue} />
                        </td>
                      </tr>
                    ))}
                    {(summary.byPurpose ?? []).length === 0 && (
                      <tr>
                        <td colSpan={4} className="text-muted text-center">
                          داده‌ای نیست
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>

            <div className="col-lg-6">
              <h6>تعمیرات / قطعات / استهلاک</h6>
              <div className="table-responsive">
                <table className="table table-sm align-middle">
                  <thead>
                    <tr>
                      <th>وسیله</th>
                      <th className="text-center">تعمیر</th>
                      <th className="text-center">قطعات</th>
                      <th className="text-center">استهلاک</th>
                    </tr>
                  </thead>
                  <tbody>
                    {(summary.maintenance ?? []).map((row) => (
                      <tr key={row.vehicleId}>
                        <td>{row.vehicleLabel}</td>
                        <td className="text-center">
                          <AmountDisplay value={row.maintenanceCost} />
                        </td>
                        <td className="text-center">
                          <AmountDisplay value={row.partsCost} />
                        </td>
                        <td className="text-center">
                          <AmountDisplay value={row.depreciationCost} />
                        </td>
                      </tr>
                    ))}
                    {(summary.maintenance ?? []).length === 0 && (
                      <tr>
                        <td colSpan={4} className="text-muted text-center">
                          داده‌ای نیست
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>

            <div className="col-12">
              <h6>بر اساس وسیله / باربری</h6>
              <div className="table-responsive">
                <table className="table table-sm align-middle">
                  <thead>
                    <tr>
                      <th>وسیله / باربری</th>
                      <th className="text-center">نوع</th>
                      <th className="text-center">سفر</th>
                      <th className="text-center">تن</th>
                      <th className="text-center">کرایه خرید</th>
                      <th className="text-center">کرایه فروش</th>
                      <th className="text-center">درآمد سفر</th>
                    </tr>
                  </thead>
                  <tbody>
                    {(summary.byVehicle ?? []).map((row, idx) => (
                      <tr key={`${row.vehicleId}-${row.freightMode}-${idx}`}>
                        <td>{row.vehicleLabel}</td>
                        <td className="text-center">{row.freightModeName}</td>
                        <td className="text-center">{formatAmount(row.tripCount)}</td>
                        <td className="text-center">{formatAmount(row.totalWeightTon)}</td>
                        <td className="text-center">
                          <AmountDisplay value={row.purchaseFreightAmount} />
                        </td>
                        <td className="text-center">
                          <AmountDisplay value={row.saleFreightAmount} />
                        </td>
                        <td className="text-center">
                          <AmountDisplay value={row.totalRevenue} />
                        </td>
                      </tr>
                    ))}
                    {(summary.byVehicle ?? []).length === 0 && (
                      <tr>
                        <td colSpan={7} className="text-muted text-center">
                          داده‌ای نیست
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  )
}

export default TransportReportPage
