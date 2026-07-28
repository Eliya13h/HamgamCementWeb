import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import Icon from '../../components/common/Icon'
import JalaliDateField from '../../components/common/JalaliDateField'
import DataTable from '../../lib/dataTableSetup'
import { createServerSideTableOptions, formatAmount } from '../../lib/dataTableOptions'
import { usePageCrud } from '../../permissions/usePageCrud'
import { fetchCurrencyRates } from '../../services/currenciesApi'
import {
  bankAccountsApi,
  currencyExchangesApi,
  fetchCashBoxOptions,
} from '../../services/ledgerApi'
import {
  fetchCurrencyRateAt,
  getCurrencyRateToBase,
} from '../../services/transactionsApi'
import { fetchCurrencyOptions } from '../../services/transportApi'

const emptyForm = () => ({
  exchangeDate: new Date().toISOString().slice(0, 10),
  fromCurrencyId: '',
  fromAmount: '',
  toCurrencyId: '',
  toAmount: '',
  dealRate: '',
  recognizeFxDifference: false,
  fromVia: 'cash',
  fromCashBoxId: '',
  fromBankAccountId: '',
  toVia: 'cash',
  toCashBoxId: '',
  toBankAccountId: '',
  description: '',
})

function formatRateValue(value) {
  const n = Number(value)
  if (!Number.isFinite(n) || n <= 0) return ''
  return String(Number(n.toFixed(8)))
}

function formatAmountValue(value) {
  const n = Number(value)
  if (!Number.isFinite(n) || n < 0) return ''
  return String(Number(n.toFixed(4)))
}

/** چند واحد ارز مقصد به‌ازای ۱ واحد مبدأ — از نرخ‌های سیستم نسبت به پایه */
function systemCrossRate(fromCurrencyId, toCurrencyId, baseCurrencyId, ratesMap) {
  if (!fromCurrencyId || !toCurrencyId) return 0
  if (String(fromCurrencyId) === String(toCurrencyId)) return 0
  const fromRate = getCurrencyRateToBase(fromCurrencyId, baseCurrencyId, ratesMap)
  const toRate = getCurrencyRateToBase(toCurrencyId, baseCurrencyId, ratesMap)
  if (!toRate || toRate <= 0) return 0
  return fromRate / toRate
}

function CurrencyExchangePage() {
  const { canCreate, canDelete } = usePageCrud('/accounting/currency-exchange')
  const tableRef = useRef(null)
  const [loadError, setLoadError] = useState('')
  const [formError, setFormError] = useState('')
  const [message, setMessage] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [showForm, setShowForm] = useState(false)
  const [deleteRow, setDeleteRow] = useState(null)
  const [form, setForm] = useState(emptyForm)
  const [rateTouched, setRateTouched] = useState(false)

  const [currencies, setCurrencies] = useState([])
  const [cashBoxes, setCashBoxes] = useState([])
  const [banks, setBanks] = useState([])
  const [baseCurrencyId, setBaseCurrencyId] = useState(null)
  const [currencyRates, setCurrencyRates] = useState({})

  useEffect(() => {
    let cancelled = false
    Promise.all([
      fetchCurrencyOptions(),
      fetchCashBoxOptions(),
      bankAccountsApi.options(),
      fetchCurrencyRates().catch(() => null),
    ])
      .then(([currencyRows, boxRows, bankRows, ratesPayload]) => {
        if (cancelled) return
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
        if (ratesPayload) {
          setBaseCurrencyId(ratesPayload.baseCurrencyId ?? null)
          const map = {}
          for (const row of ratesPayload.rates ?? []) {
            map[String(row.currencyId)] = Number(row.baseUnitsPerUnit)
          }
          setCurrencyRates(map)
        }
      })
      .catch((err) => setLoadError(err.message))
    return () => {
      cancelled = true
    }
  }, [])

  const currencyLabel = useCallback(
    (id) => {
      const row = currencies.find((c) => String(c.value) === String(id))
      return row?.label || row?.currencyCode || ''
    },
    [currencies],
  )

  const applySystemRate = useCallback(
    (prev, ratesOverride) => {
      const rates = ratesOverride ?? currencyRates
      const cross = systemCrossRate(
        prev.fromCurrencyId,
        prev.toCurrencyId,
        baseCurrencyId,
        rates,
      )
      const dealRate = formatRateValue(cross)
      const fromAmount = Number(prev.fromAmount)
      const toAmount =
        dealRate && Number.isFinite(fromAmount) && fromAmount > 0
          ? formatAmountValue(fromAmount * Number(dealRate))
          : prev.toAmount
      return { ...prev, dealRate, toAmount }
    },
    [baseCurrencyId, currencyRates],
  )

  // وقتی ارز یا تاریخ عوض شد و کاربر نرخ را دستی نکرده، نرخ سیستم را بگذار
  useEffect(() => {
    if (!showForm || rateTouched) return
    if (!form.fromCurrencyId || !form.toCurrencyId) return
    if (String(form.fromCurrencyId) === String(form.toCurrencyId)) return

    let cancelled = false
    const date = form.exchangeDate || undefined

    Promise.all([
      String(form.fromCurrencyId) === String(baseCurrencyId)
        ? Promise.resolve({ baseUnitsPerUnit: 1 })
        : fetchCurrencyRateAt(form.fromCurrencyId, date).catch(() => null),
      String(form.toCurrencyId) === String(baseCurrencyId)
        ? Promise.resolve({ baseUnitsPerUnit: 1 })
        : fetchCurrencyRateAt(form.toCurrencyId, date).catch(() => null),
    ]).then(([fromSnap, toSnap]) => {
      if (cancelled) return
      const map = { ...currencyRates }
      if (fromSnap?.baseUnitsPerUnit != null) {
        map[String(form.fromCurrencyId)] = Number(fromSnap.baseUnitsPerUnit)
      }
      if (toSnap?.baseUnitsPerUnit != null) {
        map[String(form.toCurrencyId)] = Number(toSnap.baseUnitsPerUnit)
      }
      setCurrencyRates(map)
      setForm((prev) => applySystemRate(prev, map))
    })

    return () => {
      cancelled = true
    }
    // عمداً currencyRates کامل را وابسته نمی‌کنیم تا حلقه نشود
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    showForm,
    rateTouched,
    form.fromCurrencyId,
    form.toCurrencyId,
    form.exchangeDate,
    baseCurrencyId,
    applySystemRate,
  ])

  const reloadTable = useCallback(() => {
    tableRef.current?.dt()?.ajax.reload(null, false)
  }, [])

  const openCreate = () => {
    setFormError('')
    setMessage('')
    setRateTouched(false)
    const base = currencies.find((c) => c.isBaseCurrency)
    const other = currencies.find((c) => !c.isBaseCurrency)
    const initial = {
      ...emptyForm(),
      fromCurrencyId: other ? String(other.value) : '',
      toCurrencyId: base ? String(base.value) : '',
      fromCashBoxId: cashBoxes[0]?.value ?? '',
      toCashBoxId: cashBoxes[0]?.value ?? '',
    }
    setForm(applySystemRate(initial))
    setShowForm(true)
  }

  const closeModals = () => {
    setShowForm(false)
    setDeleteRow(null)
    setFormError('')
    setSubmitting(false)
    setRateTouched(false)
  }

  const handleFromAmountChange = (value) => {
    setForm((prev) => {
      const rate = Number(prev.dealRate)
      const amount = Number(value)
      const toAmount =
        rate > 0 && Number.isFinite(amount) && amount > 0
          ? formatAmountValue(amount * rate)
          : ''
      return { ...prev, fromAmount: value, toAmount }
    })
  }

  const handleDealRateChange = (value) => {
    setRateTouched(true)
    setForm((prev) => {
      const rate = Number(value)
      const amount = Number(prev.fromAmount)
      const toAmount =
        rate > 0 && Number.isFinite(amount) && amount > 0
          ? formatAmountValue(amount * rate)
          : prev.toAmount
      return { ...prev, dealRate: value, toAmount }
    })
  }

  const handleToAmountChange = (value) => {
    setRateTouched(true)
    setForm((prev) => {
      const toAmount = Number(value)
      const fromAmount = Number(prev.fromAmount)
      const dealRate =
        fromAmount > 0 && Number.isFinite(toAmount) && toAmount >= 0
          ? formatRateValue(toAmount / fromAmount)
          : prev.dealRate
      return { ...prev, toAmount: value, dealRate }
    })
  }

  const handleCurrencyChange = (field, value) => {
    setRateTouched(false)
    setForm((prev) => ({ ...prev, [field]: value }))
  }

  const resetRateFromSystem = () => {
    setRateTouched(false)
    setForm((prev) => applySystemRate(prev))
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    setSubmitting(true)
    setFormError('')
    setMessage('')

    if (String(form.fromCurrencyId) === String(form.toCurrencyId)) {
      setFormError('ارز مبدأ و مقصد باید متفاوت باشند.')
      setSubmitting(false)
      return
    }

    const fromAmount = Number(form.fromAmount)
    const toAmount = Number(form.toAmount)
    const dealRate = Number(form.dealRate)
    if (!(fromAmount > 0) || !(toAmount > 0) || !(dealRate > 0)) {
      setFormError('مبلغ مبدأ، نرخ تبدیل و مبلغ مقصد باید بزرگ‌تر از صفر باشند.')
      setSubmitting(false)
      return
    }

    const payload = {
      exchangeDate: form.exchangeDate || null,
      fromCurrencyId: Number(form.fromCurrencyId),
      fromAmount,
      toCurrencyId: Number(form.toCurrencyId),
      toAmount,
      recognizeFxDifference: !!form.recognizeFxDifference,
      fromCashBoxId:
        form.fromVia === 'cash' && form.fromCashBoxId
          ? Number(form.fromCashBoxId)
          : null,
      fromBankAccountId:
        form.fromVia === 'bank' && form.fromBankAccountId
          ? Number(form.fromBankAccountId)
          : null,
      toCashBoxId:
        form.toVia === 'cash' && form.toCashBoxId
          ? Number(form.toCashBoxId)
          : null,
      toBankAccountId:
        form.toVia === 'bank' && form.toBankAccountId
          ? Number(form.toBankAccountId)
          : null,
      description: form.description?.trim() || null,
    }

    if (!payload.fromCashBoxId && !payload.fromBankAccountId) {
      setFormError('مبدأ (صندوق یا بانک) را انتخاب کنید.')
      setSubmitting(false)
      return
    }
    if (!payload.toCashBoxId && !payload.toBankAccountId) {
      setFormError('مقصد (صندوق یا بانک) را انتخاب کنید.')
      setSubmitting(false)
      return
    }

    try {
      const result = await currencyExchangesApi.create(payload)
      setMessage(result.message ?? 'تبدیل ارز ثبت شد.')
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
      await currencyExchangesApi.remove(deleteRow.currencyExchangeTxnId)
      closeModals()
      reloadTable()
    } catch (err) {
      setFormError(err.message)
      setSubmitting(false)
    }
  }

  const tableOptions = useMemo(
    () =>
      createServerSideTableOptions({
        ajax: currencyExchangesApi.createDataTableAjax(setLoadError),
        searching: true,
        ordering: false,
        columns: [
          { data: 'rowNumber', name: 'rowNumber' },
          { data: 'exchangeDate', name: 'exchangeDate' },
          {
            data: null,
            name: 'from',
            orderable: false,
            render: (_d, _t, row) =>
              `${formatAmount(row.fromAmount)} ${row.fromCurrencyCode ?? ''}`,
          },
          {
            data: null,
            name: 'to',
            orderable: false,
            render: (_d, _t, row) =>
              `${formatAmount(row.toAmount)} ${row.toCurrencyCode ?? ''}`,
          },
          { data: 'fromWallet', name: 'fromWallet' },
          { data: 'toWallet', name: 'toWallet' },
          { data: 'modeLabel', name: 'modeLabel' },
          {
            data: 'fxDifferenceInBaseCurrency',
            name: 'fxDifferenceInBaseCurrency',
            className: 'text-end',
            render: (data) => formatAmount(data),
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
      9: (_data, _type, row) =>
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

  const fromCode = currencyLabel(form.fromCurrencyId)
  const toCode = currencyLabel(form.toCurrencyId)

  return (
    <div className="users-page">
      <div className="content-card card border-0 h-100">
        <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between gap-3 flex-wrap">
          <h2 className="card-title mb-0">خرید و فروش ارز</h2>
          {canCreate && (
            <button
              type="button"
              className="btn btn-sm btn-accent btn-users-new d-inline-flex align-items-center gap-2"
              onClick={openCreate}
            >
              <Icon name="plus" />
              <span>ثبت تبدیل ارز</span>
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
                  <th>خروج</th>
                  <th>ورود</th>
                  <th>از</th>
                  <th>به</th>
                  <th>حالت</th>
                  <th>سود/زیان پایه</th>
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
              <form className="modal-content" onSubmit={handleSubmit}>
                <div className="modal-header">
                  <h5 className="modal-title">ثبت خرید / فروش ارز</h5>
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
                      <label className="form-label">تاریخ</label>
                      <JalaliDateField
                        value={form.exchangeDate}
                        onChange={(value) => {
                          setRateTouched(false)
                          setForm((prev) => ({ ...prev, exchangeDate: value }))
                        }}
                        required
                      />
                    </div>
                    <div className="col-md-8 d-flex align-items-end">
                      <div className="form-check">
                        <input
                          className="form-check-input"
                          type="checkbox"
                          id="recognizeFxDifference"
                          checked={form.recognizeFxDifference}
                          onChange={(e) =>
                            setForm((prev) => ({
                              ...prev,
                              recognizeFxDifference: e.target.checked,
                            }))
                          }
                        />
                        <label
                          className="form-check-label"
                          htmlFor="recognizeFxDifference"
                        >
                          شناسایی سود/زیان نسبت به نرخ سیستم
                        </label>
                      </div>
                    </div>

                    <div className="col-md-6">
                      <label className="form-label">ارز مبدأ</label>
                      <select
                        className="form-select"
                        value={form.fromCurrencyId}
                        required
                        onChange={(e) =>
                          handleCurrencyChange('fromCurrencyId', e.target.value)
                        }
                      >
                        <option value="">انتخاب کنید</option>
                        {currencies.map((c) => (
                          <option key={c.value} value={c.value}>
                            {c.label}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div className="col-md-6">
                      <label className="form-label">ارز مقصد</label>
                      <select
                        className="form-select"
                        value={form.toCurrencyId}
                        required
                        onChange={(e) =>
                          handleCurrencyChange('toCurrencyId', e.target.value)
                        }
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
                      <label className="form-label">مبلغ مبدأ</label>
                      <input
                        type="number"
                        className="form-control"
                        min="0"
                        step="any"
                        value={form.fromAmount}
                        required
                        onChange={(e) => handleFromAmountChange(e.target.value)}
                      />
                    </div>
                    <div className="col-md-4">
                      <div className="d-flex align-items-center justify-content-between gap-2">
                        <label className="form-label mb-0">نرخ تبدیل</label>
                        <button
                          type="button"
                          className="btn btn-link btn-sm p-0"
                          onClick={resetRateFromSystem}
                        >
                          نرخ سیستم
                        </button>
                      </div>
                      <input
                        type="number"
                        className="form-control"
                        min="0"
                        step="any"
                        value={form.dealRate}
                        required
                        onChange={(e) => handleDealRateChange(e.target.value)}
                      />
                      <div className="form-text">
                        چند واحد {toCode || 'مقصد'} به‌ازای ۱ واحد{' '}
                        {fromCode || 'مبدأ'}
                      </div>
                    </div>
                    <div className="col-md-4">
                      <label className="form-label">مبلغ مقصد (خودکار)</label>
                      <input
                        type="number"
                        className="form-control"
                        min="0"
                        step="any"
                        value={form.toAmount}
                        required
                        onChange={(e) => handleToAmountChange(e.target.value)}
                      />
                      <div className="form-text">
                        در صورت نیاز قابل ویرایش است
                      </div>
                    </div>

                    <div className="col-12">
                      <h6 className="text-muted mb-2">مبدأ و مقصد نقدینگی</h6>
                    </div>
                    <div className="col-md-3">
                      <label className="form-label">خروج از</label>
                      <select
                        className="form-select"
                        value={form.fromVia}
                        onChange={(e) =>
                          setForm((prev) => ({
                            ...prev,
                            fromVia: e.target.value,
                          }))
                        }
                      >
                        <option value="cash">صندوق</option>
                        <option value="bank">بانک</option>
                      </select>
                    </div>
                    <div className="col-md-3">
                      <label className="form-label">
                        {form.fromVia === 'cash' ? 'صندوق مبدأ' : 'بانک مبدأ'}
                      </label>
                      {form.fromVia === 'cash' ? (
                        <select
                          className="form-select"
                          value={form.fromCashBoxId}
                          required
                          onChange={(e) =>
                            setForm((prev) => ({
                              ...prev,
                              fromCashBoxId: e.target.value,
                            }))
                          }
                        >
                          <option value="">انتخاب کنید</option>
                          {cashBoxes.map((b) => (
                            <option key={b.value} value={b.value}>
                              {b.label}
                            </option>
                          ))}
                        </select>
                      ) : (
                        <select
                          className="form-select"
                          value={form.fromBankAccountId}
                          required
                          onChange={(e) =>
                            setForm((prev) => ({
                              ...prev,
                              fromBankAccountId: e.target.value,
                            }))
                          }
                        >
                          <option value="">انتخاب کنید</option>
                          {banks.map((b) => (
                            <option key={b.value} value={b.value}>
                              {b.label}
                            </option>
                          ))}
                        </select>
                      )}
                    </div>
                    <div className="col-md-3">
                      <label className="form-label">ورود به</label>
                      <select
                        className="form-select"
                        value={form.toVia}
                        onChange={(e) =>
                          setForm((prev) => ({
                            ...prev,
                            toVia: e.target.value,
                          }))
                        }
                      >
                        <option value="cash">صندوق</option>
                        <option value="bank">بانک</option>
                      </select>
                    </div>
                    <div className="col-md-3">
                      <label className="form-label">
                        {form.toVia === 'cash' ? 'صندوق مقصد' : 'بانک مقصد'}
                      </label>
                      {form.toVia === 'cash' ? (
                        <select
                          className="form-select"
                          value={form.toCashBoxId}
                          required
                          onChange={(e) =>
                            setForm((prev) => ({
                              ...prev,
                              toCashBoxId: e.target.value,
                            }))
                          }
                        >
                          <option value="">انتخاب کنید</option>
                          {cashBoxes.map((b) => (
                            <option key={b.value} value={b.value}>
                              {b.label}
                            </option>
                          ))}
                        </select>
                      ) : (
                        <select
                          className="form-select"
                          value={form.toBankAccountId}
                          required
                          onChange={(e) =>
                            setForm((prev) => ({
                              ...prev,
                              toBankAccountId: e.target.value,
                            }))
                          }
                        >
                          <option value="">انتخاب کنید</option>
                          {banks.map((b) => (
                            <option key={b.value} value={b.value}>
                              {b.label}
                            </option>
                          ))}
                        </select>
                      )}
                    </div>

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
                    className="btn btn-light"
                    onClick={closeModals}
                    disabled={submitting}
                  >
                    انصراف
                  </button>
                  <button
                    type="submit"
                    className="btn btn-accent"
                    disabled={submitting}
                  >
                    {submitting ? 'در حال ثبت…' : 'ثبت'}
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
          >
            <div className="modal-dialog modal-dialog-centered">
              <div className="modal-content">
                <div className="modal-header">
                  <h5 className="modal-title">حذف سند تبدیل ارز</h5>
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
                    سند تبدیل{' '}
                    <strong>
                      {formatAmount(deleteRow.fromAmount)}{' '}
                      {deleteRow.fromCurrencyCode}
                    </strong>{' '}
                    به{' '}
                    <strong>
                      {formatAmount(deleteRow.toAmount)}{' '}
                      {deleteRow.toCurrencyCode}
                    </strong>{' '}
                    حذف شود؟ سند دفترروزنامه نیز برگشت داده می‌شود.
                  </p>
                </div>
                <div className="modal-footer">
                  <button
                    type="button"
                    className="btn btn-light"
                    onClick={closeModals}
                    disabled={submitting}
                  >
                    انصراف
                  </button>
                  <button
                    type="button"
                    className="btn btn-danger"
                    onClick={handleDelete}
                    disabled={submitting}
                  >
                    {submitting ? 'در حال حذف…' : 'حذف'}
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

export default CurrencyExchangePage
