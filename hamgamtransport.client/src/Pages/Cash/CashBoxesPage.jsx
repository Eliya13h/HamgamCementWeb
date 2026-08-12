import { useCallback, useEffect, useRef, useState } from 'react'
import Icon from '../../components/common/Icon'
import JalaliDateField from '../../components/common/JalaliDateField'
import {
  useModalKeyboardShortcuts,
  useModalAutoFocus,
} from '../../hooks/useModalKeyboardShortcuts'
import { showAppToast } from '../../lib/appToast'
import { persianValidity, validateFormPersian } from '../../lib/persianFormValidity'
import CrudTablePage from '../../components/common/CrudTablePage'
import { usePageCrud } from '../../permissions/usePageCrud'
import { fetchCurrencyOptions } from '../../services/currencyApi'
import {
  cashBoxesApi,
  fetchCashBoxOptions,
  fetchCashBoxUserOptions,
  freeCashTransfer,
} from '../../services/ledgerApi'

const columns = [
  { data: 'code', title: 'کد' },
  { data: 'name', title: 'نام' },
  { data: 'parentName', title: 'صندوق بالاتر', orderable: false },
  {
    data: 'balancesText',
    title: 'موجودی ارزها',
    orderable: false,
    defaultContent: '',
  },
  {
    data: 'userCount',
    title: 'کاربران',
    orderable: false,
    className: 'text-center',
  },
  {
    data: 'isActive',
    title: 'وضعیت',
    className: 'text-center',
    render: (data) =>
      data
        ? '<span class="badge badge-active">فعال</span>'
        : '<span class="badge badge-inactive">غیرفعال</span>',
  },
]

const fields = [
  {
    name: 'code',
    label: 'کد (خودکار)',
    type: 'text',
    col: 4,
    showOnlyOnEdit: true,
    readOnlyOnEdit: true,
  },
  { name: 'name', label: 'نام', type: 'text', required: true, col: 4 },
  {
    name: 'parentCashBoxId',
    label: 'صندوق بالاتر',
    type: 'select',
    col: 4,
    loadOptions: async () => {
      const rows = await fetchCashBoxOptions()
      return rows.map((r) => ({
        value: String(r.value),
        label: r.label,
      }))
    },
  },
  {
    name: 'userIds',
    label: 'کاربران صندوق',
    type: 'multiselect',
    col: 8,
    fromRow: (row) =>
      String(row.userIdsText ?? '')
        .split(/[,\s]+/)
        .map((x) => x.trim())
        .filter(Boolean),
    loadOptions: async () => {
      const rows = await fetchCashBoxUserOptions()
      return rows.map((r) => ({
        value: String(r.value),
        label: r.label,
      }))
    },
  },
  { name: 'description', label: 'توضیحات', type: 'textarea', col: 12 },
  { name: 'isPettyCash', label: 'تنخواه گردان', type: 'switch', default: false, col: 4 },
  {
    name: 'ceilingAmountInBase',
    label: 'سقف تنخواه (ارز پایه)',
    type: 'number',
    step: '0.01',
    col: 4,
    default: 0,
    showWhen: (form) => Boolean(form.isPettyCash),
  },
  { name: 'isActive', label: 'فعال', type: 'switch', col: 4, default: true },
]

function resolveCeilingAmount(payload) {
  if (!payload.isPettyCash) return 0
  const value = Number(payload.ceilingAmountInBase)
  return Number.isFinite(value) ? value : 0
}

const cashBoxesPageApi = {
  createDataTableAjax: cashBoxesApi.createDataTableAjax,
  create: (payload) =>
    cashBoxesApi.create({
      name: payload.name,
      parentCashBoxId: payload.parentCashBoxId,
      userIds: payload.userIds ?? [],
      description: payload.description,
      isPettyCash: payload.isPettyCash,
      ceilingAmountInBase: resolveCeilingAmount(payload),
      isActive: payload.isActive,
    }),
  update: (id, payload) =>
    cashBoxesApi.update(id, {
      name: payload.name,
      parentCashBoxId: payload.parentCashBoxId,
      userIds: payload.userIds ?? [],
      description: payload.description,
      isPettyCash: payload.isPettyCash,
      ceilingAmountInBase: resolveCeilingAmount(payload),
      isActive: payload.isActive,
    }),
  remove: cashBoxesApi.remove,
}

function buildAmountMap(currencies) {
  return Object.fromEntries(
    currencies.map((c) => [String(c.value), '']),
  )
}

function CashBoxesPage() {
  const { canCreate } = usePageCrud('/cash/boxes')
  const transferFormRef = useRef(null)
  const [showTransfer, setShowTransfer] = useState(false)
  const [tableKey, setTableKey] = useState(0)
  const [boxes, setBoxes] = useState([])
  const [currencies, setCurrencies] = useState([])
  const [transferError, setTransferError] = useState('')
  const [transferMessage, setTransferMessage] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [transferForm, setTransferForm] = useState({
    fromCashBoxId: '',
    toCashBoxId: '',
    transferDate: new Date().toISOString().slice(0, 10),
    description: '',
    amounts: {},
  })

  useEffect(() => {
    if (!showTransfer) return
    let cancelled = false
    Promise.all([fetchCashBoxOptions(), fetchCurrencyOptions()])
      .then(([boxRows, currencyRows]) => {
        if (cancelled) return
        setBoxes(
          (boxRows ?? []).map((r) => ({
            value: String(r.value),
            label: r.label,
          })),
        )
        setCurrencies(currencyRows ?? [])
        setTransferForm((prev) => ({
          ...prev,
          amounts: Object.keys(prev.amounts).length
            ? prev.amounts
            : buildAmountMap(currencyRows ?? []),
        }))
      })
      .catch((err) => setTransferError(err.message))
    return () => {
      cancelled = true
    }
  }, [showTransfer])

  const openTransfer = () => {
    setTransferError('')
    setTransferMessage('')
    setTransferForm({
      fromCashBoxId: '',
      toCashBoxId: '',
      transferDate: new Date().toISOString().slice(0, 10),
      description: '',
      amounts: buildAmountMap(currencies),
    })
    setShowTransfer(true)
  }

  const closeTransfer = useCallback(() => {
    setShowTransfer(false)
    setSubmitting(false)
    setTransferError('')
  }, [])

  const triggerTransferSave = useCallback(() => {
    if (!submitting) transferFormRef.current?.requestSubmit()
  }, [submitting])

  useModalKeyboardShortcuts({
    open: showTransfer,
    onClose: closeTransfer,
    onSave: triggerTransferSave,
    formRef: transferFormRef,
  })

  useModalAutoFocus({ open: showTransfer, formRef: transferFormRef })

  const handleTransfer = async (event) => {
    event.preventDefault()
    const formEl = event.currentTarget
    const message = validateFormPersian(formEl)
    if (message) {
      showAppToast(message)
      formEl.reportValidity()
      return
    }

    setSubmitting(true)
    setTransferError('')
    setTransferMessage('')

    if (transferForm.fromCashBoxId === transferForm.toCashBoxId) {
      const err = 'صندوق مبدأ و مقصد نمی‌توانند یکسان باشند.'
      setTransferError(err)
      showAppToast(err)
      setSubmitting(false)
      return
    }

    const lines = Object.entries(transferForm.amounts)
      .map(([currencyId, amount]) => ({
        currencyId: Number(currencyId),
        amount: Number(amount) || 0,
      }))
      .filter((l) => l.currencyId > 0 && l.amount > 0)

    if (!lines.length) {
      const err = 'حداقل یک مبلغ برای یک ارز وارد کنید.'
      setTransferError(err)
      showAppToast(err)
      setSubmitting(false)
      return
    }

    try {
      const result = await freeCashTransfer({
        fromCashBoxId: Number(transferForm.fromCashBoxId),
        toCashBoxId: Number(transferForm.toCashBoxId),
        transferDate: transferForm.transferDate || null,
        description: transferForm.description?.trim() || null,
        lines,
      })
      setTransferMessage(result.message ?? 'انتقال ثبت شد.')
      setShowTransfer(false)
      setSubmitting(false)
      setTableKey((k) => k + 1)
    } catch (err) {
      setTransferError(err.message)
      setSubmitting(false)
    }
  }

  return (
    <>
      {transferMessage && (
        <div className="alert alert-success py-2 mb-3">{transferMessage}</div>
      )}
      <CrudTablePage
        key={tableKey}
        title="صندوق‌ها"
        createLabel="صندوق جدید"
        api={cashBoxesPageApi}
        idField="cashBoxId"
        nameField="name"
        columns={columns}
        fields={fields}
        permissionPath="/cash/boxes"
        canDeleteRow={() => false}
        headerExtra={
          canCreate ? (
            <button
              type="button"
              className="btn btn-sm btn-outline-primary d-inline-flex align-items-center gap-2"
              onClick={openTransfer}
            >
              <Icon name="exchange" />
              <span>انتقال بین صندوق</span>
            </button>
          ) : null
        }
      />

      {showTransfer && (
        <>
          <div
            className="modal-backdrop show users-modal-backdrop"
            onClick={closeTransfer}
          />
          <div
            className="modal show d-block users-modal"
            tabIndex="-1"
            role="dialog"
            data-bs-focus="false"
          >
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable">
              <form
                ref={transferFormRef}
                className="modal-content"
                onSubmit={handleTransfer}
                noValidate
              >
                <div className="modal-header">
                  <h5 className="modal-title">انتقال آزاد بین صندوق‌ها</h5>
                  <button
                    type="button"
                    className="btn-close"
                    aria-label="بستن"
                    onClick={closeTransfer}
                  />
                </div>
                <div className="modal-body">
                  {transferError && (
                    <div className="alert alert-danger py-2">{transferError}</div>
                  )}
                  <div className="row g-3">
                    <div className="col-md-6">
                      <label className="form-label">از صندوق</label>
                      <select
                        className="form-select"
                        value={transferForm.fromCashBoxId}
                        required
                        {...persianValidity('لطفاً صندوق مبدأ را انتخاب کنید.')}
                        onChange={(e) =>
                          setTransferForm((prev) => ({
                            ...prev,
                            fromCashBoxId: e.target.value,
                          }))
                        }
                      >
                        <option value="">انتخاب کنید</option>
                        {boxes.map((b) => (
                          <option key={b.value} value={b.value}>
                            {b.label}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div className="col-md-6">
                      <label className="form-label">به صندوق</label>
                      <select
                        className="form-select"
                        value={transferForm.toCashBoxId}
                        required
                        {...persianValidity('لطفاً صندوق مقصد را انتخاب کنید.')}
                        onChange={(e) =>
                          setTransferForm((prev) => ({
                            ...prev,
                            toCashBoxId: e.target.value,
                          }))
                        }
                      >
                        <option value="">انتخاب کنید</option>
                        {boxes.map((b) => (
                          <option key={b.value} value={b.value}>
                            {b.label}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div className="col-md-6">
                      <label className="form-label">تاریخ</label>
                      <JalaliDateField
                        value={transferForm.transferDate}
                        onChange={(value) =>
                          setTransferForm((prev) => ({
                            ...prev,
                            transferDate: value,
                          }))
                        }
                        required
                      />
                    </div>
                    <div className="col-12">
                      <label className="form-label mb-2">مبالغ به تفکیک ارز</label>
                      {!currencies.length ? (
                        <div className="text-muted small">
                          ارزی در سیستم تعریف نشده است.
                        </div>
                      ) : (
                        <div className="d-flex flex-column gap-2">
                          {currencies.map((c) => (
                            <div
                              key={c.value}
                              className="row g-2 align-items-center"
                            >
                              <div className="col-5">
                                <span className="small">
                                  {c.label}
                                  {c.isBaseCurrency ? ' (پایه)' : ''}
                                </span>
                              </div>
                              <div className="col-7">
                                <input
                                  className="form-control form-control-sm"
                                  type="number"
                                  step="0.0001"
                                  min="0"
                                  value={
                                    transferForm.amounts[String(c.value)] ?? ''
                                  }
                                  onChange={(e) =>
                                    setTransferForm((prev) => ({
                                      ...prev,
                                      amounts: {
                                        ...prev.amounts,
                                        [String(c.value)]: e.target.value,
                                      },
                                    }))
                                  }
                                />
                              </div>
                            </div>
                          ))}
                        </div>
                      )}
                    </div>
                    <div className="col-12">
                      <label className="form-label">توضیحات</label>
                      <textarea
                        className="form-control"
                        rows={2}
                        value={transferForm.description}
                        onChange={(e) =>
                          setTransferForm((prev) => ({
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
                    onClick={closeTransfer}
                  >
                    انصراف
                  </button>
                  <button
                    type="submit"
                    className="btn btn-accent"
                    disabled={submitting}
                  >
                    {submitting ? 'در حال ثبت...' : 'ثبت انتقال'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </>
      )}
    </>
  )
}

export default CashBoxesPage
