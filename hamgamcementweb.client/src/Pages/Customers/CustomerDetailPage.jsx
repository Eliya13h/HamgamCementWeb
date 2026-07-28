import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import Icon from '../../components/common/Icon'
import DataTable from '../../lib/dataTableSetup'
import { formatJalaliDate } from '../../lib/afghanSolarCalendar'
import { createServerSideTableOptions } from '../../lib/dataTableOptions'
import AmountDisplay from '../../components/common/AmountDisplay'
import { makeAmountCurrencyRender } from '../../lib/currencyFormat'
import { usePageCrud } from '../../permissions/usePageCrud'
import { fetchBaseCurrency } from '../../services/currenciesApi'
import {
  createCustomerInvoicesDataTableAjax,
  fetchCustomer,
  postCustomerOpeningBalance,
} from '../../services/customersApi'

function statusBadgeClass(code) {
  if (code === 'debtor') return 'badge-debtor'
  if (code === 'creditor') return 'badge-creditor'
  return 'badge-settled'
}

function CustomerDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const customerId = Number(id)
  const tableRef = useRef(null)
  const { canEdit } = usePageCrud('/transactions/sale')
  const { canEdit: canEditCustomer } = usePageCrud('/people/customers')
  const [customer, setCustomer] = useState(null)
  const [loadError, setLoadError] = useState('')
  const [tableError, setTableError] = useState('')
  const [actionError, setActionError] = useState('')
  const [actionMessage, setActionMessage] = useState('')
  const [postingOpening, setPostingOpening] = useState(false)
  const [invoiceTotals, setInvoiceTotals] = useState({ totalPurchase: 0, totalPayment: 0 })
  const [loading, setLoading] = useState(true)
  const [baseCurrencySymbol, setBaseCurrencySymbol] = useState('')
  const currencySymbolRef = useRef('')

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
    currencySymbolRef.current = baseCurrencySymbol
    const dt = tableRef.current?.dt()
    if (dt && baseCurrencySymbol) {
      dt.rows().invalidate('data').draw(false)
    }
  }, [baseCurrencySymbol])

  const amountCurrencyRender = useMemo(
    () => makeAmountCurrencyRender(() => currencySymbolRef.current),
    [],
  )

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

  const handleInvoiceLoaded = useCallback((json) => {
    setInvoiceTotals({
      totalPurchase: json.totalPurchase ?? 0,
      totalPayment: json.totalPayment ?? 0,
    })
    if (json.currencySymbol) {
      currencySymbolRef.current = json.currencySymbol
      setBaseCurrencySymbol(json.currencySymbol)
    }
  }, [])

  const tableOptions = useMemo(
    () =>
      createServerSideTableOptions({
        ajax: createCustomerInvoicesDataTableAjax(
          customerId,
          setTableError,
          handleInvoiceLoaded,
        ),
        order: [[2, 'desc']],
        columns: [
          { data: 'rowNumber', name: 'rowNumber' },
          { data: 'invoiceNumber', name: 'invoiceNumber' },
          {
            data: 'invoiceDate',
            name: 'invoiceDate',
            render: (data) => formatJalaliDate(data),
          },
          { data: 'itemsCount', name: 'itemsCount', className: 'text-center' },
          { data: 'totalAmount', name: 'totalAmount', render: amountCurrencyRender },
          { data: 'paidAmount', name: 'paidAmount', render: amountCurrencyRender },
          {
            data: 'statusName',
            name: 'statusName',
            render: (data) => `<span class="badge badge-invoice-status">${data ?? '—'}</span>`,
          },
          { data: null, name: 'actions', defaultContent: '' },
        ],
        columnDefs: [
          {
            targets: 0,
            orderable: false,
            searchable: false,
            width: '56px',
            className: 'text-center',
          },
          { targets: [3, 4, 5, 6], className: 'text-center' },
          {
            targets: 7,
            orderable: false,
            searchable: false,
            className: 'text-center all dt-actions-col',
            width: '150px',
          },
        ],
      }),
    [customerId, handleInvoiceLoaded, amountCurrencyRender],
  )

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

  const actionSlots = useMemo(
    () => ({
      7: (_data, _type, row) => (
        <div className="dt-actions">
          <button
            type="button"
            className="dt-action-btn"
            title="نمایش فاکتور"
            disabled
            onClick={() => navigate(`/transactions/sale/${row.saleInvoiceId}`)}
          >
            <Icon name="eye" />
          </button>
          <button type="button" className="dt-action-btn" title="چاپ" disabled>
            <Icon name="print" />
          </button>
          <button type="button" className="dt-action-btn" title="برگشت" disabled>
            <Icon name="rotate-left" />
          </button>
          {canEdit && !row.isPosted && (
            <button
              type="button"
              className="dt-action-btn"
              title="ویرایش"
              onClick={() => navigate(`/transactions/sale?edit=${row.saleInvoiceId}`)}
            >
              <Icon name="edit" />
            </button>
          )}
        </div>
      ),
    }),
    [canEdit, navigate],
  )

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
      <div className="content-card card border-0 mb-3">
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
              <span className="customer-detail-label">کل خرید</span>
              <span className="customer-detail-value">
                <AmountDisplay value={customer.totalPurchase} symbol={baseCurrencySymbol} />
              </span>
            </div>
            <div className="customer-detail-item">
              <span className="customer-detail-label">کل پرداخت</span>
              <span className="customer-detail-value">
                <AmountDisplay value={customer.totalPayment} symbol={baseCurrencySymbol} />
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

      <div className="content-card card border-0 h-100">
        <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0">
          <h3 className="card-title h5 mb-0">فاکتورهای فروش</h3>
        </div>
        <div className="card-body card-body-table">
          {tableError && (
            <div className="alert alert-danger py-2 users-load-error mb-0">{tableError}</div>
          )}
          <div className="users-table-wrapper">
            <DataTable
              ref={tableRef}
              className="table table-hover w-100 align-middle"
              options={tableOptions}
              slots={actionSlots}
            >
              <thead>
                <tr>
                  <th>#</th>
                  <th>کد فاکتور</th>
                  <th>تاریخ فاکتور</th>
                  <th>تعداد اقلام</th>
                  <th>جمع کل</th>
                  <th>مبلغ پرداختی</th>
                  <th>وضعیت</th>
                  <th>عملیات</th>
                </tr>
              </thead>
              <tfoot>
                <tr className="customer-invoice-totals-row">
                  <th colSpan={4} className="text-end">
                    مجموع
                  </th>
                  <th className="text-center" data-footer="purchase">
                    <AmountDisplay
                      value={invoiceTotals.totalPurchase}
                      symbol={baseCurrencySymbol}
                    />
                  </th>
                  <th className="text-center" data-footer="payment">
                    <AmountDisplay
                      value={invoiceTotals.totalPayment}
                      symbol={baseCurrencySymbol}
                    />
                  </th>
                  <th colSpan={2} />
                </tr>
              </tfoot>
            </DataTable>
          </div>
        </div>
      </div>
    </div>
  )
}

export default CustomerDetailPage
