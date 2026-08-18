import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import Icon from '../../components/common/Icon'
import AmountDisplay from '../../components/common/AmountDisplay'
import { usePageCrud } from '../../permissions/usePageCrud'
import { fetchBaseCurrency } from '../../services/currenciesApi'
import { fetchCustomer, postCustomerOpeningBalance } from '../../services/customersApi'

function statusBadgeClass(code) {
  if (code === 'debtor') return 'badge-debtor'
  if (code === 'creditor') return 'badge-creditor'
  return 'badge-settled'
}

function CustomerDetailPage() {
  const { id } = useParams()
  const customerId = Number(id)
  const { canEdit: canEditCustomer } = usePageCrud('/people/customers')
  const [customer, setCustomer] = useState(null)
  const [loadError, setLoadError] = useState('')
  const [actionError, setActionError] = useState('')
  const [actionMessage, setActionMessage] = useState('')
  const [postingOpening, setPostingOpening] = useState(false)
  const [loading, setLoading] = useState(true)
  const [baseCurrencySymbol, setBaseCurrencySymbol] = useState('')

  useEffect(() => {
    let cancelled = false
    fetchBaseCurrency()
      .then((currency) => {
        if (!cancelled) setBaseCurrencySymbol(currency?.symbol ?? '')
      })
      .catch(() => {
        if (!cancelled) setBaseCurrencySymbol('')
      })
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    if (!customerId) {
      setLoadError('شناسه مشتری نامعتبر است.')
      setLoading(false)
      return
    }

    setLoading(true)
    fetchCustomer(customerId)
      .then((data) => {
        setCustomer(data)
        setLoadError('')
      })
      .catch((error) => {
        setCustomer(null)
        setLoadError(error.message)
      })
      .finally(() => setLoading(false))
  }, [customerId])

  const handlePostOpeningBalance = async () => {
    if (!customerId) return
    setActionError('')
    setActionMessage('')
    setPostingOpening(true)
    try {
      const result = await postCustomerOpeningBalance(customerId)
      setActionMessage(result?.message || 'مانده اولیه در دفتر ثبت شد.')
    } catch (error) {
      setActionError(error.message)
    } finally {
      setPostingOpening(false)
    }
  }

  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center p-5 min-vh-50">
        <div className="spinner-border text-primary" role="status" aria-label="در حال بارگذاری" />
      </div>
    )
  }

  if (loadError || !customer) {
    return (
      <div className="content-card card border-0">
        <div className="card-body p-4">
          <div className="alert alert-danger mb-3">{loadError || 'مشتری یافت نشد.'}</div>
          <Link to="/people/customers" className="btn btn-outline-secondary">
            بازگشت به لیست مشتریان
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div className="users-page customer-detail-page">
      <div className="content-card card border-0">
        <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between gap-3 flex-wrap">
          <div className="d-flex align-items-center gap-3 flex-wrap">
            <Link
              to="/people/customers"
              className="btn btn-sm btn-outline-secondary d-inline-flex align-items-center gap-2"
            >
              <Icon name="arrow-right" />
              <span>بازگشت</span>
            </Link>
            <h2 className={`card-title mb-0 ${customer.isDeleted ? 'text-decoration-line-through' : ''}`}>
              {customer.name}
            </h2>
            {customer.isDeleted && (
              <span className="badge badge-inactive">حذف‌شده</span>
            )}
          </div>
          <div className="d-flex align-items-center gap-2 flex-wrap">
            {canEditCustomer && Number(customer.initialBalance) !== 0 && (
              <button
                type="button"
                className="btn btn-sm btn-outline-primary"
                onClick={handlePostOpeningBalance}
                disabled={postingOpening}
              >
                {postingOpening
                  ? 'در حال ثبت...'
                  : 'ثبت مانده اولیه در دفتر'}
              </button>
            )}
            <span className={`badge ${statusBadgeClass(customer.accountStatusCode)}`}>
              {customer.accountStatus}
            </span>
          </div>
        </div>

        <div className="card-body px-4 pb-4">
          {actionError && (
            <div className="alert alert-danger py-2">{actionError}</div>
          )}
          {actionMessage && (
            <div className="alert alert-success py-2">{actionMessage}</div>
          )}
          <div className="customer-detail-grid">
            <div className="customer-detail-item">
              <span className="customer-detail-label">تلفن</span>
              <span className="customer-detail-value" dir="ltr">
                {customer.phoneNumber || '—'}
              </span>
            </div>
            <div className="customer-detail-item">
              <span className="customer-detail-label">نوع</span>
              <span className="customer-detail-value">{customer.customerTypeName}</span>
            </div>
            <div className="customer-detail-item">
              <span className="customer-detail-label">شهر</span>
              <span className="customer-detail-value">{customer.city || '—'}</span>
            </div>
            <div className="customer-detail-item">
              <span className="customer-detail-label">کشور</span>
              <span className="customer-detail-value">{customer.country || '—'}</span>
            </div>
            <div className="customer-detail-item customer-detail-item-wide">
              <span className="customer-detail-label">آدرس</span>
              <span className="customer-detail-value">{customer.address || '—'}</span>
            </div>
            <div className="customer-detail-item">
              <span className="customer-detail-label">موجودی اولیه</span>
              <span className="customer-detail-value">
                <AmountDisplay value={customer.initialBalance} symbol={baseCurrencySymbol} />
              </span>
            </div>
            <div className="customer-detail-item">
              <span className="customer-detail-label">کل عواید</span>
              <span className="customer-detail-value">
                <AmountDisplay value={customer.totalPurchase} symbol={baseCurrencySymbol} />
              </span>
            </div>
            <div className="customer-detail-item">
              <span className="customer-detail-label">کل دریافت</span>
              <span className="customer-detail-value">
                <AmountDisplay value={customer.totalPayment} symbol={baseCurrencySymbol} />
              </span>
            </div>
            <div className="customer-detail-item">
              <span className="customer-detail-label">بالانس</span>
              <span className="customer-detail-value">
                <AmountDisplay value={customer.balance} symbol={baseCurrencySymbol} />
              </span>
            </div>
            <div className="customer-detail-item">
              <span className="customer-detail-label">وضعیت حساب</span>
              <span className="customer-detail-value">{customer.accountStatus}</span>
            </div>
            <div className="customer-detail-item">
              <span className="customer-detail-label">وضعیت فعالیت</span>
              <span className="customer-detail-value">
                {customer.isActive ? 'فعال' : 'غیرفعال'}
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

export default CustomerDetailPage
