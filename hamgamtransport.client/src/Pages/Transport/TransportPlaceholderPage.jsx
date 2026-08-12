import { useLocation } from 'react-router-dom'
import Icon from '../../components/common/Icon'

const PAGE_LABELS = {
  '/transport/vehicles': 'کشنده و بونکر',
  '/transport/trips': 'سرویس‌ها',
  '/transport/drivers': 'رانندگان',
  '/transport/owners': 'مالکان',
  '/transport/routes': 'مسیرها',
  '/transport/invoices': 'فاکتور حمل',
}

function TransportPlaceholderPage() {
  const { pathname } = useLocation()
  const title = PAGE_LABELS[pathname] ?? 'حمل‌ونقل'

  return (
    <div className="container-fluid py-4">
      <div className="card border-0 shadow-sm">
        <div className="card-body text-center py-5">
          <div className="text-primary mb-3">
            <Icon name="transactions" className="fs-1" />
          </div>
          <h1 className="h4 mb-2">{title}</h1>
          <p className="text-muted mb-0">
            این بخش در نسخهٔ اولیهٔ همگام ترانسپورت اسکفولد شده و به‌زودی تکمیل می‌شود.
          </p>
        </div>
      </div>
    </div>
  )
}

export default TransportPlaceholderPage
