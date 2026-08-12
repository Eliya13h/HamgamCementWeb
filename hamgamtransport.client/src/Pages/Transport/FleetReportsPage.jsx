import { useEffect, useState } from 'react'
import { fleetReportsApi } from '../../services/transportApi'
import { formatAmount } from '../../lib/dataTableOptions'
import { showAppToast } from '../../lib/appToast'

export default function FleetReportsPage() {
  const [vehiclePl, setVehiclePl] = useState([])
  const [ownerBalances, setOwnerBalances] = useState([])
  const [customerAr, setCustomerAr] = useState([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    setLoading(true)
    Promise.all([
      fleetReportsApi.vehiclePl(),
      fleetReportsApi.ownerBalances(),
      fleetReportsApi.customerAr(),
    ])
      .then(([pl, owners, ar]) => {
        setVehiclePl(pl ?? [])
        setOwnerBalances(owners ?? [])
        setCustomerAr(ar ?? [])
      })
      .catch((err) => showAppToast(err.message, 'danger'))
      .finally(() => setLoading(false))
  }, [])

  if (loading) {
    return (
      <div className="d-flex justify-content-center p-5">
        <div className="spinner-border text-primary" role="status" />
      </div>
    )
  }

  return (
    <div className="page-content">
      <h1 className="page-title mb-4">گزارشات ناوگان</h1>

      <div className="card mb-4">
        <div className="card-header">سود و زیان هر وسیله</div>
        <div className="card-body table-responsive">
          <table className="table table-sm">
            <thead>
              <tr>
                <th>پلاک</th>
                <th>مالک</th>
                <th>درآمد</th>
                <th>هزینه</th>
                <th>خالص</th>
              </tr>
            </thead>
            <tbody>
              {vehiclePl.map((r) => (
                <tr key={r.vehicleId}>
                  <td>{r.plateNumber}</td>
                  <td>{r.ownerName}</td>
                  <td>{formatAmount(r.revenue)}</td>
                  <td>{formatAmount(r.expenses)}</td>
                  <td>{formatAmount(r.netProfit)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <div className="card mb-4">
        <div className="card-header">مانده مالکان</div>
        <div className="card-body table-responsive">
          <table className="table table-sm">
            <thead>
              <tr>
                <th>مالک</th>
                <th>تعلق‌گرفته</th>
                <th>پرداخت‌شده</th>
                <th>مانده</th>
              </tr>
            </thead>
            <tbody>
              {ownerBalances.map((r) => (
                <tr key={r.vehicleOwnerId}>
                  <td>{r.ownerName}</td>
                  <td>{formatAmount(r.accrued)}</td>
                  <td>{formatAmount(r.paid)}</td>
                  <td>{formatAmount(r.balance)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <div className="card">
        <div className="card-header">مطالبات مشتریان (حمل)</div>
        <div className="card-body table-responsive">
          <table className="table table-sm">
            <thead>
              <tr>
                <th>مشتری</th>
                <th>درآمد سفر</th>
                <th>دریافت‌شده</th>
                <th>مانده</th>
              </tr>
            </thead>
            <tbody>
              {customerAr.map((r) => (
                <tr key={r.customerId}>
                  <td>{r.customerName}</td>
                  <td>{formatAmount(r.tripRevenue)}</td>
                  <td>{formatAmount(r.received)}</td>
                  <td>{formatAmount(r.balance)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
