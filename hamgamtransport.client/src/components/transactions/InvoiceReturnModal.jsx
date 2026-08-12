import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import AmountDisplay from '../common/AmountDisplay'
import AmountField from '../common/AmountField'
import JalaliDateField from '../common/JalaliDateField'
import PrefixNumberField from '../common/PrefixNumberField'
import {
  useModalAutoFocus,
  useModalKeyboardShortcuts,
} from '../../hooks/useModalKeyboardShortcuts'
import { showAppToast } from '../../lib/appToast'
import { persianValidity, validateFormPersian } from '../../lib/persianFormValidity'
import { todayGregorianIso } from '../../lib/afghanSolarCalendar'
import { fetchBaseCurrency, fetchCurrencyRates } from '../../services/currenciesApi'
import { fetchMeaurmentOptions } from '../../services/productsApi'
import { fetchCurrencyOptions } from '../../services/currencyApi'
import {
  calcLineTotals,
  convertAmountFromBase,
  convertAmountToBase,
  fetchCurrencyRateAt,
  getCurrencyRateToBase,
  sumTotals,
} from '../../services/transactionsApi'
import { formatAmount, formatJalaliDate } from '../common/CrudTablePage'
import '../../styles/purchase-invoice-lines.css'

function mapReturnableLine(line, sourceRate, sourceCurrencyId, baseCurrencyId) {
  const unitPrice = Number(line.unitPrice) || 0
  const unitPriceInBase =
    sourceCurrencyId && String(sourceCurrencyId) !== String(baseCurrencyId)
      ? unitPrice * (Number(sourceRate) || 1)
      : unitPrice

  return {
    referenceItemId: line.referenceItemId,
    productId: line.productId,
    productName: line.productName,
    productCode: line.productCode,
    meaurmentId: line.meaurmentId,
    meaurmentName: line.meaurmentName,
    meaurmentSymbol: line.meaurmentSymbol,
    originalQuantity: Number(line.originalQuantity) || 0,
    returnedQuantity: Number(line.returnedQuantity) || 0,
    returnableQuantity: Number(line.returnableQuantity) || 0,
    returnQty: '',
    unitPrice: unitPrice ? String(unitPrice) : '',
    unitPriceInBase: unitPriceInBase || '',
  }
}

function InvoiceReturnModal({
  open,
  onClose,
  mode,
  sourceInvoiceId,
  sourceInvoiceNumber,
  onSuccess,
  api,
}) {
  const formRef = useRef(null)
  const [loading, setLoading] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')
  const [invoiceDate, setInvoiceDate] = useState(todayGregorianIso())
  const [description, setDescription] = useState('')
  const [paidAmount, setPaidAmount] = useState('')
  const [paidAmountTouched, setPaidAmountTouched] = useState(false)
  const [lines, setLines] = useState([])
  const [pastReturns, setPastReturns] = useState([])
  const [currencyId, setCurrencyId] = useState('')
  const [currencies, setCurrencies] = useState([])
  const [baseCurrencyId, setBaseCurrencyId] = useState('')
  const [baseCurrencySymbol, setBaseCurrencySymbol] = useState('')
  const [currencyRates, setCurrencyRates] = useState({})
  const [rateSnapshot, setRateSnapshot] = useState(null)
  const [exchangeRate, setExchangeRate] = useState('')
  const [exchangeRateTouched, setExchangeRateTouched] = useState(false)
  const [sourceCurrencyId, setSourceCurrencyId] = useState('')
  const [meaurments, setMeaurments] = useState([])

  const title =
    mode === 'purchase'
      ? `برگشت از خرید — فاکتور ${sourceInvoiceNumber ?? ''}`
      : `برگشت از فروش — فاکتور ${sourceInvoiceNumber ?? ''}`

  const paidAmountLabel =
    mode === 'purchase' ? 'مبلغ دریافتی از تأمین‌کننده' : 'مبلغ پرداخت‌شده به مشتری'

  const currencySymbolById = useMemo(
    () => Object.fromEntries(currencies.map((c) => [String(c.value), c.symbol ?? ''])),
    [currencies],
  )

  const invoiceCurrencySymbol = currencySymbolById[String(currencyId)] ?? ''

  const isNonBaseCurrency = Boolean(rateSnapshot && !rateSnapshot.isBaseCurrency)

  const computedLines = useMemo(
    () =>
      calcLineTotals(
        lines.map((line) => ({
          ...line,
          quantity: line.returnQty,
        })),
        rateSnapshot,
        exchangeRate,
        meaurments,
        baseCurrencyId,
        currencyId,
      ),
    [lines, rateSnapshot, exchangeRate, meaurments, baseCurrencyId, currencyId],
  )

  const totals = useMemo(() => sumTotals(computedLines), [computedLines])

  const paidAmountNumeric = Number(paidAmount) || 0
  const remainingAmount = Math.max(0, totals.total - paidAmountNumeric)
  const isCashInvoice = totals.total > 0 && paidAmountNumeric >= totals.total

  const selectedLines = useMemo(
    () => computedLines.filter((line) => Number(line.returnQty) > 0),
    [computedLines],
  )

  const loadData = useCallback(async () => {
    if (!sourceInvoiceId) return
    setLoading(true)
    setError('')
    try {
      const [sourceInvoice, returnable, history, currencyOptions, baseCurrency, ratesData, meaurmentOptions] =
        await Promise.all([
          api.getById(sourceInvoiceId),
          api.fetchReturnableLines(sourceInvoiceId),
          api.fetchReturns(sourceInvoiceId),
          fetchCurrencyOptions().catch(() => []),
          fetchBaseCurrency().catch(() => null),
          fetchCurrencyRates().catch(() => null),
          fetchMeaurmentOptions().catch(() => []),
        ])

      setMeaurments(meaurmentOptions ?? [])

      const sourceCurrId = String(sourceInvoice.currencyId ?? '')
      const baseId = String(baseCurrency?.currencyID ?? sourceInvoice.baseCurrencyId ?? '')
      setCurrencies(currencyOptions ?? [])
      setBaseCurrencyId(baseId)
      setBaseCurrencySymbol(baseCurrency?.symbol ?? '')
      setSourceCurrencyId(sourceCurrId)
      setCurrencyId(sourceCurrId)

      if (ratesData) {
        const map = {}
        for (const row of ratesData.rates ?? []) {
          map[String(row.currencyId)] = row.baseUnitsPerUnit
        }
        setCurrencyRates(map)
      }

      const sourceRate =
        sourceInvoice.baseUnitsPerUnitAtTransaction ??
        ratesData?.rates?.find((r) => String(r.currencyId) === sourceCurrId)?.baseUnitsPerUnit ??
        1

      setLines((returnable ?? []).map((line) => mapReturnableLine(line, sourceRate, sourceCurrId, baseId)))
      setPastReturns(history ?? [])
    } catch (err) {
      setError(err.message)
      setLines([])
      setPastReturns([])
    } finally {
      setLoading(false)
    }
  }, [api, sourceInvoiceId])

  useEffect(() => {
    if (!open) return
    setInvoiceDate(todayGregorianIso())
    setDescription('')
    setPaidAmount('')
    setPaidAmountTouched(false)
    setExchangeRate('')
    setExchangeRateTouched(false)
    setRateSnapshot(null)
    loadData()
  }, [open, loadData])

  useEffect(() => {
    if (!currencyId || !open) {
      setRateSnapshot(null)
      setExchangeRate('')
      return
    }
    fetchCurrencyRateAt(currencyId, invoiceDate || undefined)
      .then((snapshot) => {
        setRateSnapshot(snapshot)
        const rate = snapshot.isBaseCurrency ? '1' : String(snapshot.baseUnitsPerUnit ?? '')
        if (!exchangeRateTouched) {
          setExchangeRate(rate)
        }
        if (!exchangeRateTouched) {
          setLines((prev) =>
            prev.map((line) => {
              if (line.unitPriceInBase == null || line.unitPriceInBase === '') return line
              return {
                ...line,
                unitPrice: convertAmountFromBase(
                  line.unitPriceInBase,
                  currencyId,
                  baseCurrencyId,
                  rate,
                ),
              }
            }),
          )
        }
      })
      .catch(() => {
        setRateSnapshot(null)
        setExchangeRate('')
      })
  }, [currencyId, invoiceDate, baseCurrencyId, exchangeRateTouched, open])

  useEffect(() => {
    if (paidAmountTouched || !open) return
    if (totals.total > 0) {
      setPaidAmount(String(totals.total))
    }
  }, [paidAmountTouched, totals.total, open])

  const handleCurrencyChange = (newCurrencyId) => {
    const oldCurrencyId = currencyId
    if (oldCurrencyId && newCurrencyId && oldCurrencyId !== newCurrencyId) {
      const oldRate = getCurrencyRateToBase(
        oldCurrencyId,
        baseCurrencyId,
        currencyRates,
        exchangeRate,
      )
      const newRate = getCurrencyRateToBase(newCurrencyId, baseCurrencyId, currencyRates)

      setLines((prev) =>
        prev.map((line) => {
          const priceInBase =
            line.unitPriceInBase != null && line.unitPriceInBase !== ''
              ? Number(line.unitPriceInBase)
              : convertAmountToBase(line.unitPrice, oldCurrencyId, baseCurrencyId, oldRate)

          if (!priceInBase && line.unitPrice === '') {
            return { ...line }
          }

          return {
            ...line,
            unitPriceInBase: priceInBase || line.unitPriceInBase || '',
            unitPrice: convertAmountFromBase(
              priceInBase,
              newCurrencyId,
              baseCurrencyId,
              newRate,
            ),
          }
        }),
      )
      setExchangeRate(String(newCurrencyId) === String(baseCurrencyId) ? '1' : String(newRate))
      setExchangeRateTouched(false)
    }
    setCurrencyId(newCurrencyId)
  }

  const handleExchangeRateChange = (value) => {
    setExchangeRateTouched(true)
    setExchangeRate(value)
    setLines((prev) =>
      prev.map((line) => {
        if (line.unitPriceInBase == null || line.unitPriceInBase === '') return line
        return {
          ...line,
          unitPrice: convertAmountFromBase(
            line.unitPriceInBase,
            currencyId,
            baseCurrencyId,
            value,
          ),
        }
      }),
    )
  }

  const handleLineQtyChange = (referenceItemId, value) => {
    setLines((prev) =>
      prev.map((line) =>
        line.referenceItemId === referenceItemId ? { ...line, returnQty: value } : line,
      ),
    )
    setPaidAmountTouched(false)
  }

  const handleLinePriceChange = (referenceItemId, value) => {
    const rate = getCurrencyRateToBase(currencyId, baseCurrencyId, currencyRates, exchangeRate)
    setLines((prev) =>
      prev.map((line) => {
        if (line.referenceItemId !== referenceItemId) return line
        return {
          ...line,
          unitPrice: value,
          unitPriceInBase:
            value === '' ? '' : convertAmountToBase(value, currencyId, baseCurrencyId, rate),
        }
      }),
    )
    setPaidAmountTouched(false)
  }

  const fillAll = () => {
    setLines((prev) =>
      prev.map((line) => ({
        ...line,
        returnQty: line.returnableQuantity > 0 ? String(line.returnableQuantity) : '',
      })),
    )
    setPaidAmountTouched(false)
  }

  const clearQuantities = () => {
    setLines((prev) => prev.map((line) => ({ ...line, returnQty: '' })))
    setPaidAmount('')
    setPaidAmountTouched(false)
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    setError('')

    const formEl = event.currentTarget
    const message = validateFormPersian(formEl)
    if (message) {
      showAppToast(message)
      formEl.reportValidity()
      return
    }

    const items = selectedLines.map((line) => ({
      referenceItemId: line.referenceItemId,
      quantity: Number(line.returnQty),
      meaurmentId: line.meaurmentId,
      unitPrice: Number(line.unitPrice) || 0,
    }))

    if (items.length === 0 && lines.length > 0) {
      setError('حداقل یک ردیف با مقدار برگشت انتخاب کنید، یا «برگشت کامل» را بزنید.')
      return
    }

    const paid = Number(paidAmount) || 0
    if (paid < 0) {
      setError('مبلغ پرداخت نمی‌تواند منفی باشد.')
      return
    }
    if (totals.total > 0 && paid > totals.total) {
      setError('مبلغ پرداخت نمی‌تواند بیشتر از جمع برگشت باشد.')
      return
    }

    setSubmitting(true)
    try {
      const payload = {
        invoiceDate,
        description: description.trim() || null,
        paidAmount: paid,
        currencyId: Number(currencyId),
        items,
      }
      const rate = Number(exchangeRate)
      if (rate > 0) {
        payload.baseUnitsPerUnit = rate
      }
      const result = await api.createReturn(sourceInvoiceId, payload)
      onSuccess?.(result)
      onClose()
    } catch (err) {
      setError(err.message)
    } finally {
      setSubmitting(false)
    }
  }

  const triggerSave = useCallback(() => {
    if (!submitting && !loading) {
      formRef.current?.requestSubmit()
    }
  }, [submitting, loading])

  useModalKeyboardShortcuts({
    open,
    onClose,
    onSave: triggerSave,
    formRef,
  })

  useModalAutoFocus({ open, formRef })

  if (!open) return null

  const canSubmit = !loading && lines.length > 0 && !submitting

  return (
    <>
      <div className="modal-backdrop show users-modal-backdrop" onClick={onClose} />
      <div className="modal show d-block users-modal" tabIndex="-1" data-bs-focus="false">
        <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable modal-xl">
          <form
            id="invoice-return-form"
            ref={formRef}
            className="modal-content"
            noValidate
            onSubmit={handleSubmit}
          >
            <div className="modal-header">
              <h5 className="modal-title">{title}</h5>
              <button type="button" className="btn-close" aria-label="بستن" onClick={onClose} />
            </div>
            <div className="modal-body">
              {error && <div className="alert alert-danger py-2">{error}</div>}

              <div className="row g-3 mb-3">
                <div className="col-md-3">
                  <label className="form-label">تاریخ برگشت (شمسی)</label>
                  <JalaliDateField
                    value={invoiceDate}
                    onChange={setInvoiceDate}
                    required
                    requiredMessage="لطفاً تاریخ برگشت را انتخاب کنید."
                  />
                </div>
                <div className="col-md-3">
                  <label className="form-label">ارز برگشت</label>
                  <select
                    className="form-select"
                    value={currencyId}
                    required
                    disabled={loading}
                    onChange={(e) => handleCurrencyChange(e.target.value)}
                    {...persianValidity('لطفاً ارز برگشت را انتخاب کنید.')}
                  >
                    <option value="">انتخاب کنید...</option>
                    {currencies.map((o) => (
                      <option key={o.value} value={o.value}>
                        {o.label}
                      </option>
                    ))}
                  </select>
                  {sourceCurrencyId && currencyId && sourceCurrencyId !== String(currencyId) && (
                    <small className="text-muted d-block mt-1">
                      ارز فاکتور مبدأ متفاوت است — قیمت‌ها با نرخ جاری تبدیل شدند
                    </small>
                  )}
                </div>
                <div className="col-md-3">
                  <label className="form-label">نرخ به ارز پایه</label>
                  {isNonBaseCurrency ? (
                    <input
                      type="number"
                      min="0"
                      step="any"
                      className="form-control"
                      value={exchangeRate}
                      required
                      disabled={loading}
                      onChange={(e) => handleExchangeRateChange(e.target.value)}
                      {...persianValidity('لطفاً نرخ ارز را وارد کنید.')}
                    />
                  ) : (
                    <input
                      type="text"
                      className="form-control"
                      readOnly
                      value={
                        rateSnapshot
                          ? rateSnapshot.isBaseCurrency
                            ? 'ارز پایه (۱:۱)'
                            : formatAmount(rateSnapshot.baseUnitsPerUnit)
                          : currencyId
                            ? 'در حال بارگذاری...'
                            : '—'
                      }
                    />
                  )}
                </div>
                <div className="col-md-3">
                  <label className="form-label">کل برگشت</label>
                  <AmountField
                    value={totals.total}
                    onChange={() => {}}
                    symbol={invoiceCurrencySymbol}
                    readOnly
                  />
                </div>
                <div className="col-md-3">
                  <label className="form-label">{paidAmountLabel}</label>
                  <AmountField
                    value={paidAmount}
                    onChange={(next) => {
                      setPaidAmountTouched(true)
                      setPaidAmount(next)
                    }}
                    symbol={invoiceCurrencySymbol}
                    min="0"
                    max={totals.total > 0 ? String(totals.total) : undefined}
                  />
                  {totals.total > 0 && (
                    <small className={`text-muted d-block mt-1${isCashInvoice ? '' : ' text-warning'}`}>
                      {isCashInvoice ? (
                        'برگشت نقدی — کل مبلغ تسویه می‌شود'
                      ) : (
                        <>
                          برگشت نسیه — مانده:{' '}
                          <AmountDisplay value={remainingAmount} symbol={invoiceCurrencySymbol} />
                        </>
                      )}
                    </small>
                  )}
                </div>
                <div className="col-md-9">
                  <label className="form-label">توضیحات</label>
                  <input
                    type="text"
                    className="form-control"
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    placeholder="اختیاری"
                  />
                </div>
              </div>

              {pastReturns.length > 0 && (
                <div className="mb-3">
                  <h6 className="mb-2">سوابق برگشت این فاکتور</h6>
                  <div className="table-responsive">
                    <table className="table table-sm table-bordered mb-0">
                      <thead>
                        <tr>
                          <th>شماره برگشت</th>
                          <th>تاریخ</th>
                          <th>مبلغ</th>
                          <th>وضعیت</th>
                        </tr>
                      </thead>
                      <tbody>
                        {pastReturns.map((row) => (
                          <tr key={row.invoiceId}>
                            <td>{row.invoiceNumber}</td>
                            <td>{formatJalaliDate(row.invoiceDate)}</td>
                            <td>{formatAmount(row.totalAmount)}</td>
                            <td>
                              {row.isPosted ? (
                                <span className="badge badge-active">ثبت‌شده</span>
                              ) : (
                                <span className="badge badge-inactive">پیش‌نویس</span>
                              )}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}

              <div className="d-flex align-items-center justify-content-between gap-2 mb-2 flex-wrap">
                <h6 className="mb-0">ردیف‌های قابل برگشت</h6>
                <div className="d-flex gap-2">
                  <button
                    type="button"
                    className="btn btn-sm btn-outline-secondary"
                    onClick={fillAll}
                    disabled={loading || lines.length === 0}
                  >
                    برگشت کامل
                  </button>
                  <button
                    type="button"
                    className="btn btn-sm btn-outline-secondary"
                    onClick={clearQuantities}
                    disabled={loading || lines.length === 0}
                  >
                    پاک کردن مقادیر
                  </button>
                </div>
              </div>

              {loading ? (
                <p className="text-muted mb-0">در حال بارگذاری...</p>
              ) : lines.length === 0 ? (
                <p className="text-muted mb-0">ردیف قابل برگشتی برای این فاکتور باقی نمانده است.</p>
              ) : (
                <div className="table-responsive">
                  <table className="table align-middle purchase-lines-table table-sm table-bordered mb-0">
                    <colgroup>
                      <col className="col-product" />
                      <col className="col-unit" />
                      <col className="col-qty" />
                      <col className="col-qty" />
                      <col className="col-qty" />
                      <col className="col-qty" />
                      <col className="col-price" />
                      <col className="col-total" />
                      <col className="col-total-base" />
                    </colgroup>
                    <thead>
                      <tr>
                        <th className="col-product">محصول</th>
                        <th className="col-unit">واحد</th>
                        <th className="col-qty">مقدار اصلی</th>
                        <th className="col-qty">برگشت‌شده</th>
                        <th className="col-qty">قابل برگشت</th>
                        <th className="col-qty">مقدار برگشت</th>
                        <th className="col-price">قیمت واحد ({invoiceCurrencySymbol || '—'})</th>
                        <th className="col-total">جمع ({invoiceCurrencySymbol || '—'})</th>
                        <th className="col-total-base">جمع ({baseCurrencySymbol || '—'})</th>
                      </tr>
                    </thead>
                    <tbody>
                      {computedLines.map((line) => {
                        const unitLabel = line.meaurmentSymbol || line.meaurmentName || ''
                        const qty = Number(line.returnQty) || 0
                        return (
                          <tr key={line.referenceItemId}>
                            <td className="col-product">
                              <div>{line.productName}</div>
                              <small className="text-muted">{line.productCode}</small>
                            </td>
                            <td className="col-unit">
                              <input
                                type="text"
                                className="form-control form-control-sm invoice-line-control-height"
                                value={unitLabel}
                                readOnly
                              />
                            </td>
                            <td className="col-qty text-center">
                              {formatAmount(line.originalQuantity)}
                            </td>
                            <td className="col-qty text-center text-warning">
                              {formatAmount(line.returnedQuantity)}
                            </td>
                            <td className="col-qty text-center">
                              {formatAmount(line.returnableQuantity)}
                            </td>
                            <td className="col-qty">
                              <PrefixNumberField
                                prefix={unitLabel}
                                value={line.returnQty}
                                onChange={(next) =>
                                  handleLineQtyChange(line.referenceItemId, next)
                                }
                                min="0"
                                max={line.returnableQuantity > 0 ? String(line.returnableQuantity) : undefined}
                                step="any"
                                className="amount-field-sm invoice-line-control-height"
                              />
                            </td>
                            <td className="col-price">
                              <AmountField
                                value={line.unitPrice}
                                onChange={(next) =>
                                  handleLinePriceChange(line.referenceItemId, next)
                                }
                                symbol={invoiceCurrencySymbol}
                                className="amount-field-sm invoice-line-control-height"
                                min="0"
                                step="any"
                              />
                            </td>
                            <td className="col-total text-center">
                              {qty > 0 ? (
                                <AmountDisplay value={line.lineTotal} symbol={invoiceCurrencySymbol} />
                              ) : (
                                '—'
                              )}
                            </td>
                            <td className="col-total-base text-center">
                              {qty > 0 ? (
                                <AmountDisplay value={line.lineTotalBase} symbol={baseCurrencySymbol} />
                              ) : (
                                '—'
                              )}
                            </td>
                          </tr>
                        )
                      })}
                    </tbody>
                    {selectedLines.length > 0 && (
                      <tfoot>
                        <tr>
                          <th colSpan={7} className="text-end">
                            جمع انتخاب‌شده
                          </th>
                          <th className="text-center">
                            <AmountDisplay value={totals.total} symbol={invoiceCurrencySymbol} />
                          </th>
                          <th className="text-center">
                            <AmountDisplay value={totals.totalBase} symbol={baseCurrencySymbol} />
                          </th>
                        </tr>
                      </tfoot>
                    )}
                  </table>
                </div>
              )}
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" onClick={onClose}>
                انصراف
              </button>
              <button type="submit" className="btn btn-accent" disabled={!canSubmit}>
                {submitting ? 'در حال ثبت...' : 'ثبت برگشت'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </>
  )
}

export default InvoiceReturnModal
