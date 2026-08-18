import { useEffect, useState } from 'react'
import JalaliDateField from '../../components/common/JalaliDateField'
import {
  costCentersApi,
  getCostCenterReportUrl,
} from '../../services/ledgerApi'

export default function CostCentersReportPage() {
  const [dateFrom, setDateFrom] = useState('')
  const [dateTo, setDateTo] = useState('')
  const [costCenterId, setCostCenterId] = useState('')
  const [centers, setCenters] = useState([])
  const [error, setError] = useState('')

  useEffect(() => {
    costCentersApi
      .options()
      .then((rows) => setCenters(rows ?? []))
      .catch((e) => setError(e.message))
  }, [])

  const handleOpen = () => {
    setError('')
    const url = getCostCenterReportUrl({
      dateFrom,
      dateTo,
      costCenterId: costCenterId || undefined,
    })
    window.open(url, '_blank', 'noopener,noreferrer')
  }

  return (
    <div className="users-page">
      <div className="content-card card border-0">
        <div className="card-header bg-transparent border-0 pt-4 px-4">
          <h2 className="card-title mb-1">گزارش مراکز هزینه</h2>
          <p className="text-muted mb-0 small">
            خلاصه و جزئیات خطوط دفتر به تفکیک مرکز هزینه — چاپ HTML مانند روزنامچه عمومی.
          </p>
        </div>
        <div className="card-body p-4">
          {error ? <div className="alert alert-danger py-2">{error}</div> : null}
          <div className="row g-3 align-items-end">
            <div className="col-md-3">
              <label className="form-label">از تاریخ</label>
              <JalaliDateField value={dateFrom} onChange={setDateFrom} />
            </div>
            <div className="col-md-3">
              <label className="form-label">تا تاریخ</label>
              <JalaliDateField value={dateTo} onChange={setDateTo} />
            </div>
            <div className="col-md-3">
              <label className="form-label">مرکز هزینه</label>
              <select
                className="form-select"
                value={costCenterId}
                onChange={(e) => setCostCenterId(e.target.value)}
              >
                <option value="">همه مراکز</option>
                {centers.map((c) => (
                  <option key={c.value} value={c.value}>
                    {c.label}
                  </option>
                ))}
              </select>
            </div>
            <div className="col-md-3">
              <button type="button" className="btn btn-accent w-100" onClick={handleOpen}>
                چاپ / PDF گزارش
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
