import { useEffect, useMemo, useRef, useState } from 'react'
import DataTable from '../../lib/dataTableSetup'
import { createServerSideTableOptions } from '../../lib/dataTableOptions'
import { fetchCurrencyOptions } from '../../services/transportApi'
import {
  cashShiftsApi,
  closeCashShift,
  fetchCashBoxBalances,
  fetchCashBoxOptions,
  openCashShift,
} from '../../services/ledgerApi'

function buildAmountMap(currencies, seed = {}) {
  return Object.fromEntries(
    currencies.map((c) => [String(c.value), seed[String(c.value)] ?? '0']),
  )
}

function mapLines(amountMap) {
  return Object.entries(amountMap)
    .map(([currencyId, amount]) => ({
      currencyId: Number(currencyId),
      amount: Number(amount) || 0,
    }))
    .filter((l) => l.currencyId > 0 && l.amount !== 0)
}

function CurrencyAmountRows({ currencies, amounts, onChange, label }) {
  if (!currencies.length) {
    return <div className="text-muted small">ارزی در سیستم تعریف نشده است.</div>
  }

  return (
    <div className="d-flex flex-column gap-2">
      <label className="form-label mb-0">{label}</label>
      {currencies.map((c) => (
        <div key={c.value} className="row g-2 align-items-center">
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
              value={amounts[String(c.value)] ?? '0'}
              onChange={(e) =>
                onChange({
                  ...amounts,
                  [String(c.value)]: e.target.value,
                })
              }
            />
          </div>
        </div>
      ))}
    </div>
  )
}

function CashShiftsPage() {
  const tableRef = useRef(null)
  const [loadError, setLoadError] = useState('')
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  const [boxes, setBoxes] = useState([])
  const [currencies, setCurrencies] = useState([])
  const [cashBoxId, setCashBoxId] = useState('')
  const [openingAmounts, setOpeningAmounts] = useState({})
  const [closeId, setCloseId] = useState('')
  const [transferAmounts, setTransferAmounts] = useState({})
  const [bookBalances, setBookBalances] = useState([])
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    Promise.all([fetchCashBoxOptions(), fetchCurrencyOptions()])
      .then(([boxRows, currencyRows]) => {
        setBoxes(
          boxRows.map((r) => ({
            value: String(r.value),
            label: r.label,
          })),
        )
        setCurrencies(currencyRows)
        setOpeningAmounts(buildAmountMap(currencyRows))
        setTransferAmounts(buildAmountMap(currencyRows))
      })
      .catch((e) => setError(e.message))
  }, [])

  useEffect(() => {
    if (!cashBoxId) {
      setBookBalances([])
      return
    }

    fetchCashBoxBalances(Number(cashBoxId))
      .then((rows) => {
        setBookBalances(rows ?? [])
        const seed = Object.fromEntries(
          (rows ?? []).map((b) => [String(b.currencyId), String(b.amount ?? 0)]),
        )
        setOpeningAmounts(buildAmountMap(currencies, seed))
      })
      .catch(() => setBookBalances([]))
  }, [cashBoxId, currencies])

  const tableOptions = useMemo(
    () =>
      createServerSideTableOptions({
        ajax: cashShiftsApi.createDataTableAjax(setLoadError),
        searching: true,
        ordering: false,
        columns: [
          { data: 'rowNumber', name: 'rowNumber' },
          { data: 'cashShiftId', name: 'cashShiftId' },
          { data: 'cashBoxName', name: 'cashBoxName' },
          { data: 'userName', name: 'userName' },
          { data: 'statusLabel', name: 'statusLabel' },
          { data: 'openedAt', name: 'openedAt' },
          { data: 'closedAt', name: 'closedAt' },
          {
            data: 'openingLinesText',
            name: 'openingLinesText',
            defaultContent: '',
          },
          {
            data: 'transferLinesText',
            name: 'transferLinesText',
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
        ],
      }),
    [],
  )

  const reloadTable = () => {
    tableRef.current?.dt?.()?.ajax?.reload?.(null, false)
  }

  const onOpen = async (event) => {
    event.preventDefault()
    setSubmitting(true)
    setMessage('')
    setError('')
    try {
      const result = await openCashShift({
        cashBoxId: Number(cashBoxId),
        openingLines: mapLines(openingAmounts),
      })
      setMessage(result.message)
      reloadTable()
    } catch (err) {
      setError(err.message)
    } finally {
      setSubmitting(false)
    }
  }

  const onClose = async (event) => {
    event.preventDefault()
    setSubmitting(true)
    setMessage('')
    setError('')
    try {
      const result = await closeCashShift(Number(closeId), {
        transferLines: mapLines(transferAmounts),
      })
      setMessage(result.message)
      setTransferAmounts(buildAmountMap(currencies))
      reloadTable()
    } catch (err) {
      setError(err.message)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="users-page">
      <div className="content-card content-card-fit card border-0 mb-3">
        <div className="card-body p-4">
          <h2 className="card-title mb-3">شیفت صندوق</h2>
          {message && <div className="alert alert-success py-2">{message}</div>}
          {error && <div className="alert alert-danger py-2">{error}</div>}
          <div className="row g-4 align-items-start">
            <div className="col-md-6">
              <div className="cash-shift-panel h-auto">
                <h3 className="h6">باز کردن شیفت</h3>
                <form onSubmit={onOpen} className="row g-2">
                  <div className="col-12">
                    <label className="form-label">صندوق</label>
                    <select
                      className="form-select"
                      value={cashBoxId}
                      onChange={(e) => setCashBoxId(e.target.value)}
                      required
                    >
                      <option value="">انتخاب کنید</option>
                      {boxes.map((b) => (
                        <option key={b.value} value={b.value}>
                          {b.label}
                        </option>
                      ))}
                    </select>
                  </div>
                  {bookBalances.length > 0 && (
                    <div className="col-12">
                      <div className="small text-muted">
                        موجودی دفتری:{' '}
                        {bookBalances
                          .filter((b) => Number(b.amount) !== 0)
                          .map(
                            (b) =>
                              `${b.currencyCode}:${Number(b.amount).toLocaleString('fa-IR')}`,
                          )
                          .join(' | ') || 'صفر'}
                      </div>
                    </div>
                  )}
                  <div className="col-12">
                    <CurrencyAmountRows
                      currencies={currencies}
                      amounts={openingAmounts}
                      onChange={setOpeningAmounts}
                      label="موجودی اعلامی ابتدای شیفت (به تفکیک ارز)"
                    />
                  </div>
                  <div className="col-12">
                    <button
                      type="submit"
                      className="btn btn-primary"
                      disabled={submitting}
                    >
                      باز کردن
                    </button>
                  </div>
                </form>
              </div>
            </div>
            <div className="col-md-6">
              <div className="cash-shift-panel h-auto">
                <h3 className="h6">بستن و تحویل به صندوق بالاتر</h3>
                <form onSubmit={onClose} className="row g-2">
                  <div className="col-12">
                    <label className="form-label">شناسه شیفت</label>
                    <input
                      className="form-control"
                      value={closeId}
                      onChange={(e) => setCloseId(e.target.value)}
                      required
                    />
                  </div>
                  <div className="col-12">
                    <CurrencyAmountRows
                      currencies={currencies}
                      amounts={transferAmounts}
                      onChange={setTransferAmounts}
                      label="مبالغ تحویلی (هر ارز جدا؛ بدون تبدیل)"
                    />
                  </div>
                  <div className="col-12">
                    <button
                      type="submit"
                      className="btn btn-warning"
                      disabled={submitting}
                    >
                      بستن شیفت
                    </button>
                  </div>
                </form>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div className="content-card content-card-fill card border-0">
        <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0">
          <h2 className="card-title mb-0">سابقه شیفت‌ها</h2>
        </div>
        <div className="card-body card-body-table">
          {loadError && (
            <div className="alert alert-danger py-2 mb-0">{loadError}</div>
          )}
          <div className="users-table-wrapper">
            <DataTable
              ref={tableRef}
              className="table table-hover w-100 align-middle"
              options={tableOptions}
            >
              <thead>
                <tr>
                  <th>#</th>
                  <th>شناسه</th>
                  <th>صندوق</th>
                  <th>کاربر</th>
                  <th>وضعیت</th>
                  <th>شروع</th>
                  <th>پایان</th>
                  <th>افتتاح (ارزها)</th>
                  <th>تحویل (ارزها)</th>
                </tr>
              </thead>
            </DataTable>
          </div>
        </div>
      </div>
    </div>
  )
}

export default CashShiftsPage
