import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import Icon from '../../components/common/Icon'
import JalaliDateField from '../../components/common/JalaliDateField'
import {
  useModalAutoFocus,
  useModalKeyboardShortcuts,
  usePageCreateShortcut,
} from '../../hooks/useModalKeyboardShortcuts'
import { showAppToast } from '../../lib/appToast'
import DataTable from '../../lib/dataTableSetup'
import { createServerSideTableOptions, formatAmount } from '../../lib/dataTableOptions'
import { persianValidity, validateFormPersian } from '../../lib/persianFormValidity'
import { usePageCrud } from '../../permissions/usePageCrud'
import {
  bankAccountsApi,
  fetchCashBoxOptions,
  invoiceInstallmentsApi,
  settlementsApi,
} from '../../services/ledgerApi'
import {
  fetchCustomerOptions,
  fetchSupplierOptions,
} from '../../services/transactionsApi'
import { fetchCurrencyOptions } from '../../services/currencyApi'

const PARTY_CUSTOMER = 1
const PARTY_SUPPLIER = 2

const emptyForm = () => ({
  partyType: String(PARTY_CUSTOMER),
  partyId: '',
  settlementDate: new Date().toISOString().slice(0, 10),
  currencyId: '',
  amount: '',
  paymentVia: 'cash',
  cashBoxId: '',
  bankAccountId: '',
  saleInvoiceId: '',
  purchaseInvoiceId: '',
  installmentId: '',
  description: '',
})

function PartySettlementsPage() {
  const { canCreate, canDelete } = usePageCrud('/accounting/settlements')
  const tableRef = useRef(null)
  const formRef = useRef(null)
  const [loadError, setLoadError] = useState('')
  const [formError, setFormError] = useState('')
  const [message, setMessage] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [showForm, setShowForm] = useState(false)
  const [deleteRow, setDeleteRow] = useState(null)
  const [form, setForm] = useState(emptyForm)

  const [customers, setCustomers] = useState([])
  const [suppliers, setSuppliers] = useState([])
  const [currencies, setCurrencies] = useState([])
  const [cashBoxes, setCashBoxes] = useState([])
  const [banks, setBanks] = useState([])
  const [installments, setInstallments] = useState([])

  useEffect(() => {
    let cancelled = false
    Promise.all([
      fetchCustomerOptions(),
      fetchSupplierOptions(),
      fetchCurrencyOptions(),
      fetchCashBoxOptions(),
      bankAccountsApi.options(),
    ])
      .then(([customerRows, supplierRows, currencyRows, boxRows, bankRows]) => {
        if (cancelled) return
        setCustomers(customerRows ?? [])
        setSuppliers(supplierRows ?? [])
        setCurrencies(currencyRows ?? [])
        setCashBoxes(
          (boxRows ?? []).map((r) => ({
            value: String(r.value),
            label: r.label,
          })),
        )
        setBanks(
          (bankRows ?? []).map((r) => ({
            value: String(r.value),
            label: r.label,
          })),
        )
        const base = (currencyRows ?? []).find((c) => c.isBaseCurrency)
        if (base) {
          setForm((prev) => ({
            ...prev,
            currencyId: prev.currencyId || String(base.value),
          }))
        }
      })
      .catch((err) => setLoadError(err.message))
    return () => {
      cancelled = true
    }
  }, [])

  const partyOptions =
    Number(form.partyType) === PARTY_SUPPLIER ? suppliers : customers

  useEffect(() => {
    const invoiceId = Number(form.partyType) === PARTY_CUSTOMER ? form.saleInvoiceId : form.purchaseInvoiceId
    if (!invoiceId) {
      setInstallments([])
      return
    }
    invoiceInstallmentsApi
      .list(Number(form.partyType) === PARTY_CUSTOMER ? 1 : 2, invoiceId)
      .then((items) => setInstallments(items.filter((item) => Number(item.remaining) > 0)))
      .catch(() => setInstallments([]))
  }, [form.partyType, form.saleInvoiceId, form.purchaseInvoiceId])

  const reloadTable = useCallback(() => {
    tableRef.current?.dt()?.ajax.reload(null, false)
  }, [])

  const openCreate = useCallback(() => {
    setFormError('')
    setMessage('')
    const base = currencies.find((c) => c.isBaseCurrency)
    setForm({
      ...emptyForm(),
      currencyId: base ? String(base.value) : '',
    })
    setShowForm(true)
  }, [currencies])

  const closeModals = useCallback(() => {
    setShowForm(false)
    setDeleteRow(null)
    setFormError('')
    setSubmitting(false)
  }, [])

  const handleSubmit = async (event) => {
    event.preventDefault()
    const formEl = event.currentTarget
    const message = validateFormPersian(formEl)
    if (message) {
      showAppToast(message)
      formEl.reportValidity()
      return
    }

    setSubmitting(true)
    setFormError('')
    setMessage('')

    const partyType = Number(form.partyType)
    const payload = {
      partyType,
      partyId: Number(form.partyId),
      settlementDate: form.settlementDate || null,
      currencyId: Number(form.currencyId),
      amount: Number(form.amount),
      cashBoxId:
        form.paymentVia === 'cash' && form.cashBoxId
          ? Number(form.cashBoxId)
          : null,
      bankAccountId:
        form.paymentVia === 'bank' && form.bankAccountId
          ? Number(form.bankAccountId)
          : null,
      saleInvoiceId:
        partyType === PARTY_CUSTOMER && form.saleInvoiceId
          ? Number(form.saleInvoiceId)
          : null,
      purchaseInvoiceId:
        partyType === PARTY_SUPPLIER && form.purchaseInvoiceId
          ? Number(form.purchaseInvoiceId)
          : null,
      installmentId: form.installmentId ? Number(form.installmentId) : null,
      description: form.description?.trim() || null,
    }

    if (!payload.cashBoxId && !payload.bankAccountId) {
      setFormError('صندوق یا حساب بانکی را انتخاب کنید.')
      setSubmitting(false)
      return
    }

    try {
      const result = await settlementsApi.create(payload)
      setMessage(result.message ?? 'تسویه ثبت شد.')
      closeModals()
      reloadTable()
    } catch (err) {
      setFormError(err.message)
      setSubmitting(false)
    }
  }

  const handleDelete = async () => {
    if (!deleteRow) return
    setSubmitting(true)
    setFormError('')
    try {
      await settlementsApi.remove(deleteRow.partySettlementId)
      closeModals()
      reloadTable()
    } catch (err) {
      setFormError(err.message)
      setSubmitting(false)
    }
  }

  const triggerSave = useCallback(() => {
    if (!submitting) {
      formRef.current?.requestSubmit()
    }
  }, [submitting])

  useModalKeyboardShortcuts({
    open: showForm,
    onClose: closeModals,
    onSave: triggerSave,
    formRef,
  })

  useModalKeyboardShortcuts({
    open: Boolean(deleteRow),
    onClose: closeModals,
  })

  usePageCreateShortcut({
    enabled: canCreate,
    onNew: openCreate,
    isBlocked: showForm || Boolean(deleteRow),
  })

  useModalAutoFocus({ open: showForm, formRef })

  const tableOptions = useMemo(
    () =>
      createServerSideTableOptions({
        ajax: settlementsApi.createDataTableAjax(setLoadError),
        searching: true,
        ordering: false,
        columns: [
          { data: 'rowNumber', name: 'rowNumber' },
          { data: 'settlementDate', name: 'settlementDate' },
          { data: 'partyTypeLabel', name: 'partyTypeLabel' },
          { data: 'partyName', name: 'partyName' },
          { data: 'currencyCode', name: 'currencyCode' },
          {
            data: 'amount',
            name: 'amount',
            className: 'text-end',
            render: (data) => formatAmount(data),
          },
          {
            data: null,
            name: 'source',
            orderable: false,
            render: (_d, _t, row) =>
              row.cashBoxName || row.bankAccountName || '—',
          },
          {
            data: 'description',
            name: 'description',
            defaultContent: '',
            orderable: false,
          },
          {
            data: null,
            name: 'actions',
            orderable: false,
            searchable: false,
            className: 'text-center',
            defaultContent: '',
          },
        ],
        columnDefs: [
          {
            targets: 0,
            orderable: false,
            searchable: false,
            width: '56px',
            className: 'text-center',
          },
          {
            targets: -1,
            orderable: false,
            searchable: false,
            className: 'text-center all dt-actions-col',
            width: '80px',
          },
        ],
      }),
    [],
  )

  const actionSlots = useMemo(
    () => ({
      8: (_data, _type, row) =>
        canDelete ? (
          <div className="dt-actions">
            <button
              type="button"
              className="dt-action-btn btn-delete"
              title="حذف"
              onClick={() => {
                setFormError('')
                setDeleteRow(row)
              }}
            >
              <Icon name="trash" />
            </button>
          </div>
        ) : null,
    }),
    [canDelete],
  )

  return (
    <div className="users-page">
      <div className="content-card card border-0 h-100">
        <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between gap-3 flex-wrap">
          <h2 className="card-title mb-0">دریافت و پرداخت</h2>
          {canCreate && (
            <button
              type="button"
              className="btn btn-sm btn-accent btn-users-new d-inline-flex align-items-center gap-2"
              title="ثبت دریافت/پرداخت (Ctrl+N)"
              onClick={openCreate}
            >
              <Icon name="plus" />
              <span>ثبت دریافت/پرداخت</span>
            </button>
          )}
        </div>

        <div className="card-body card-body-table">
          {message && (
            <div className="alert alert-success py-2 mb-3">{message}</div>
          )}
          {loadError && (
            <div className="alert alert-danger py-2 users-load-error mb-0">
              {loadError}
            </div>
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
                  <th>تاریخ</th>
                  <th>نوع</th>
                  <th>طرف حساب</th>
                  <th>ارز</th>
                  <th>مبلغ</th>
                  <th>از/به</th>
                  <th>توضیحات</th>
                  <th>عملیات</th>
                </tr>
              </thead>
            </DataTable>
          </div>
        </div>
      </div>

      {showForm && (
        <>
          <div
            className="modal-backdrop show users-modal-backdrop"
            onClick={closeModals}
          />
          <div
            className="modal show d-block users-modal"
            tabIndex="-1"
            role="dialog"
            data-bs-focus="false"
          >
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable modal-lg">
              <form
                ref={formRef}
                className="modal-content"
                noValidate
                onSubmit={handleSubmit}
              >
                <div className="modal-header">
                  <h5 className="modal-title">ثبت دریافت / پرداخت</h5>
                  <button
                    type="button"
                    className="btn-close"
                    aria-label="بستن"
                    onClick={closeModals}
                  />
                </div>
                <div className="modal-body">
                  {formError && (
                    <div className="alert alert-danger py-2">{formError}</div>
                  )}
                  <div className="row g-3">
                    <div className="col-md-4">
                      <label className="form-label">نوع طرف حساب</label>
                      <select
                        className="form-select"
                        value={form.partyType}
                        required
                        onChange={(e) =>
                          setForm((prev) => ({
                            ...prev,
                            partyType: e.target.value,
                            partyId: '',
                            saleInvoiceId: '',
                            purchaseInvoiceId: '',
                            installmentId: '',
                          }))
                        }
                        {...persianValidity('لطفاً نوع طرف حساب را انتخاب کنید.')}
                      >
                        <option value={PARTY_CUSTOMER}>مشتری (دریافت)</option>
                        <option value={PARTY_SUPPLIER}>
                          تأمین‌کننده (پرداخت)
                        </option>
                      </select>
                    </div>
                    <div className="col-md-4">
                      <label className="form-label">طرف حساب</label>
                      <select
                        className="form-select"
                        value={form.partyId}
                        required
                        onChange={(e) =>
                          setForm((prev) => ({
                            ...prev,
                            partyId: e.target.value,
                          }))
                        }
                        {...persianValidity('لطفاً طرف حساب را انتخاب کنید.')}
                      >
                        <option value="">انتخاب کنید</option>
                        {partyOptions.map((p) => (
                          <option key={p.value} value={p.value}>
                            {p.label}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div className="col-md-4">
                      <label className="form-label">تاریخ</label>
                      <JalaliDateField
                        value={form.settlementDate}
                        onChange={(value) =>
                          setForm((prev) => ({
                            ...prev,
                            settlementDate: value,
                          }))
                        }
                        required
                        requiredMessage="لطفاً تاریخ را انتخاب کنید."
                      />
                    </div>
                    <div className="col-md-4">
                      <label className="form-label">ارز</label>
                      <select
                        className="form-select"
                        value={form.currencyId}
                        required
                        onChange={(e) =>
                          setForm((prev) => ({
                            ...prev,
                            currencyId: e.target.value,
                          }))
                        }
                        {...persianValidity('لطفاً ارز را انتخاب کنید.')}
                      >
                        <option value="">انتخاب کنید</option>
                        {currencies.map((c) => (
                          <option key={c.value} value={c.value}>
                            {c.label}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div className="col-md-4">
                      <label className="form-label">مبلغ</label>
                      <input
                        type="number"
                        className="form-control"
                        min="0"
                        step="0.0001"
                        value={form.amount}
                        required
                        onChange={(e) =>
                          setForm((prev) => ({
                            ...prev,
                            amount: e.target.value,
                          }))
                        }
                        {...persianValidity('لطفاً مبلغ را وارد کنید.')}
                      />
                    </div>
                    <div className="col-md-4">
                      <label className="form-label">روش پرداخت</label>
                      <select
                        className="form-select"
                        value={form.paymentVia}
                        onChange={(e) =>
                          setForm((prev) => ({
                            ...prev,
                            paymentVia: e.target.value,
                            cashBoxId: '',
                            bankAccountId: '',
                          }))
                        }
                      >
                        <option value="cash">صندوق</option>
                        <option value="bank">حساب بانکی</option>
                      </select>
                    </div>
                    {form.paymentVia === 'cash' ? (
                      <div className="col-md-6">
                        <label className="form-label">صندوق</label>
                        <select
                          className="form-select"
                          value={form.cashBoxId}
                          required
                          onChange={(e) =>
                            setForm((prev) => ({
                              ...prev,
                              cashBoxId: e.target.value,
                            }))
                          }
                          {...persianValidity('لطفاً صندوق را انتخاب کنید.')}
                        >
                          <option value="">انتخاب کنید</option>
                          {cashBoxes.map((b) => (
                            <option key={b.value} value={b.value}>
                              {b.label}
                            </option>
                          ))}
                        </select>
                      </div>
                    ) : (
                      <div className="col-md-6">
                        <label className="form-label">حساب بانکی</label>
                        <select
                          className="form-select"
                          value={form.bankAccountId}
                          required
                          onChange={(e) =>
                            setForm((prev) => ({
                              ...prev,
                              bankAccountId: e.target.value,
                            }))
                          }
                          {...persianValidity('لطفاً حساب بانکی را انتخاب کنید.')}
                        >
                          <option value="">انتخاب کنید</option>
                          {banks.map((b) => (
                            <option key={b.value} value={b.value}>
                              {b.label}
                            </option>
                          ))}
                        </select>
                      </div>
                    )}
                    {Number(form.partyType) === PARTY_CUSTOMER ? (
                      <div className="col-md-6">
                        <label className="form-label">
                          شناسه فاکتور فروش (اختیاری)
                        </label>
                        <input
                          type="number"
                          className="form-control"
                          min="1"
                          step="1"
                          value={form.saleInvoiceId}
                          onChange={(e) =>
                            setForm((prev) => ({
                              ...prev,
                              saleInvoiceId: e.target.value,
                            }))
                          }
                        />
                      </div>
                    ) : (
                      <div className="col-md-6">
                        <label className="form-label">
                          شناسه فاکتور خرید (اختیاری)
                        </label>
                        <input
                          type="number"
                          className="form-control"
                          min="1"
                          step="1"
                          value={form.purchaseInvoiceId}
                          onChange={(e) =>
                            setForm((prev) => ({
                              ...prev,
                              purchaseInvoiceId: e.target.value,
                            }))
                          }
                        />
                      </div>
                    )}
                    {installments.length > 0 && (
                      <div className="col-md-6">
                        <label className="form-label">قسط (اختیاری)</label>
                        <select className="form-select" value={form.installmentId} onChange={(e) => setForm((prev) => ({ ...prev, installmentId: e.target.value }))}>
                          <option value="">انتخاب همه/بدون قسط</option>
                          {installments.map((item) => <option key={item.invoiceInstallmentId} value={item.invoiceInstallmentId}>قسط {item.installmentNo} — مانده {formatAmount(item.remaining)}</option>)}
                        </select>
                      </div>
                    )}
                    <div className="col-12">
                      <label className="form-label">توضیحات</label>
                      <textarea
                        className="form-control"
                        rows={2}
                        value={form.description}
                        onChange={(e) =>
                          setForm((prev) => ({
                            ...prev,
                            description: e.target.value,
                          }))
                        }
                      />
                    </div>
                  </div>
                </div>
                <div className="modal-footer">
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    onClick={closeModals}
                  >
                    انصراف
                  </button>
                  <button
                    type="submit"
                    className="btn btn-accent"
                    disabled={submitting}
                  >
                    {submitting ? 'در حال ذخیره...' : 'ثبت'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </>
      )}

      {deleteRow && (
        <>
          <div
            className="modal-backdrop show users-modal-backdrop"
            onClick={closeModals}
          />
          <div
            className="modal show d-block users-modal"
            tabIndex="-1"
            role="dialog"
            data-bs-focus="false"
          >
            <div className="modal-dialog modal-dialog-centered">
              <div className="modal-content">
                <div className="modal-header">
                  <h5 className="modal-title">حذف تسویه</h5>
                  <button
                    type="button"
                    className="btn-close"
                    aria-label="بستن"
                    onClick={closeModals}
                  />
                </div>
                <div className="modal-body">
                  {formError && (
                    <div className="alert alert-danger py-2">{formError}</div>
                  )}
                  <p className="mb-0">
                    آیا از حذف تسویه{' '}
                    <strong>
                      {deleteRow.partyName} —{' '}
                      {formatAmount(deleteRow.amount)}
                    </strong>{' '}
                    اطمینان دارید؟
                  </p>
                </div>
                <div className="modal-footer">
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    onClick={closeModals}
                  >
                    انصراف
                  </button>
                  <button
                    type="button"
                    className="btn btn-danger"
                    onClick={handleDelete}
                    disabled={submitting}
                  >
                    {submitting ? 'در حال حذف...' : 'حذف'}
                  </button>
                </div>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  )
}

export default PartySettlementsPage
