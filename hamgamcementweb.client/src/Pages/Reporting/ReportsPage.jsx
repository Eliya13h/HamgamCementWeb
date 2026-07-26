import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import AmountDisplay from '../../components/common/AmountDisplay'
import Icon from '../../components/common/Icon'
import JalaliDateField from '../../components/common/JalaliDateField'
import {
  currentJalaliYearMonth,
  getJalaliYearRange,
  toLatinIsoDate,
  todayGregorianIso,
} from '../../lib/afghanSolarCalendar'
import {
  fetchBalanceSheet,
  fetchCashBoxesOverview,
  fetchProfitAndLoss,
} from '../../services/ledgerApi'

function CurrencyAmountList({ currencies }) {
  if (!currencies?.length) return null

  return (
    <ul className="statement-currency-list">
      {currencies.map((c) => (
        <li key={c.currencyId} className="statement-currency-row">
          <span className="statement-currency-code">
            {c.currencyCode || c.symbol || c.currencyName}
            {c.isBaseCurrency ? <span className="cash-balance-base">پایه</span> : null}
          </span>
          <AmountDisplay value={c.amount} symbol={c.symbol || c.currencyCode} />
        </li>
      ))}
    </ul>
  )
}

function StatementLineList({ rows, emptyText }) {
  if (!rows?.length) {
    return <p className="statement-empty mb-0">{emptyText}</p>
  }

  return (
    <ul className="statement-line-list">
      {rows.map((row) => (
        <li key={row.accountId} className="statement-account-item">
          <div className="statement-line-row">
            <span className="statement-line-name">
              <span className="statement-line-code">{row.code}</span>
              {row.name}
            </span>
            <AmountDisplay value={row.amountInBase ?? row.amount} />
          </div>
          <CurrencyAmountList currencies={row.currencies} />
        </li>
      ))}
    </ul>
  )
}

function CurrencyTotalsTable({ rows, mode }) {
  if (!rows?.length) return null

  const isPl = mode === 'pl'

  return (
    <div className="statement-currency-summary mb-4">
      <h4 className="statement-section-title mb-2">خلاصه به تفکیک ارز</h4>
      <div className="table-responsive">
        <table className="table table-sm align-middle statement-currency-table mb-0">
          <thead>
            <tr>
              <th>ارز</th>
              {isPl ? (
                <>
                  <th>درآمد</th>
                  <th>بهای تمام‌شده</th>
                  <th>هزینه</th>
                  <th>سود/زیان</th>
                </>
              ) : (
                <>
                  <th>دارایی</th>
                  <th>بدهی</th>
                  <th>حقوق مالکانه</th>
                  <th>سود/زیان جاری</th>
                </>
              )}
              <th>معادل پایه</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.currencyId}>
                <td>
                  {row.currencyCode || row.symbol}
                  {row.isBaseCurrency ? <span className="cash-balance-base">پایه</span> : null}
                </td>
                {isPl ? (
                  <>
                    <td>
                      <AmountDisplay value={row.revenue} symbol={row.symbol} />
                    </td>
                    <td>
                      <AmountDisplay value={row.cogs} symbol={row.symbol} />
                    </td>
                    <td>
                      <AmountDisplay value={row.expense} symbol={row.symbol} />
                    </td>
                    <td>
                      <AmountDisplay value={row.netIncome} symbol={row.symbol} />
                    </td>
                    <td>
                      <AmountDisplay value={row.netIncomeInBase} />
                    </td>
                  </>
                ) : (
                  <>
                    <td>
                      <AmountDisplay value={row.assets} symbol={row.symbol} />
                    </td>
                    <td>
                      <AmountDisplay value={row.liabilities} symbol={row.symbol} />
                    </td>
                    <td>
                      <AmountDisplay value={row.equityWithIncome} symbol={row.symbol} />
                    </td>
                    <td>
                      <AmountDisplay value={row.currentNetIncome} symbol={row.symbol} />
                    </td>
                    <td>
                      <AmountDisplay value={row.equityWithIncomeInBase ?? row.assetsInBase} />
                    </td>
                  </>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}

function ProfitLossCard() {
  const yearStart = useMemo(() => {
    const { year } = currentJalaliYearMonth()
    return getJalaliYearRange(year).from
  }, [])

  const [dateFrom, setDateFrom] = useState(yearStart)
  const [dateTo, setDateTo] = useState(todayGregorianIso())
  const [appliedFrom, setAppliedFrom] = useState(yearStart)
  const [appliedTo, setAppliedTo] = useState(todayGregorianIso())
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let cancelled = false

    async function load() {
      setLoading(true)
      setError('')
      try {
        const result = await fetchProfitAndLoss({
          dateFrom: toLatinIsoDate(appliedFrom) || appliedFrom,
          dateTo: toLatinIsoDate(appliedTo) || appliedTo,
        })
        if (!cancelled) setData(result)
      } catch (err) {
        if (!cancelled) {
          setData(null)
          setError(err.message || 'بارگذاری سود و زیان با خطا مواجه شد.')
        }
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    load()
    return () => {
      cancelled = true
    }
  }, [appliedFrom, appliedTo])

  const applyFilter = () => {
    if (dateFrom && dateTo && dateFrom > dateTo) {
      setError('تاریخ شروع نباید بعد از تاریخ پایان باشد.')
      return
    }
    setAppliedFrom(dateFrom)
    setAppliedTo(dateTo)
  }

  const totals = data?.totals
  const netIncome = Number(totals?.netIncome ?? 0)
  const isProfit = netIncome >= 0

  return (
    <section className="mb-4">
      <div className="content-card card border-0">
        <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between flex-wrap gap-2">
          <div>
            <h3 className="card-title mb-1">سود و زیان</h3>
            <p className="silo-section-subtitle mb-0">
              درآمد، بهای تمام‌شده و هزینه چندارزی از دفترروزنامه؛ جمع‌ها به معادل ارز پایه
              {data?.fromLabel && data?.toLabel
                ? ` — از ${data.fromLabel} تا ${data.toLabel}`
                : ''}
            </p>
          </div>
          <Link to="/accounting/journal-entries" className="btn btn-sm btn-outline-accent">
            اسناد دفتر
          </Link>
        </div>

        <div className="card-body p-4">
          <div className="row g-3 align-items-end mb-4">
            <div className="col-md-3">
              <label className="form-label">از تاریخ</label>
              <JalaliDateField value={dateFrom} onChange={setDateFrom} />
            </div>
            <div className="col-md-3">
              <label className="form-label">تا تاریخ</label>
              <JalaliDateField value={dateTo} onChange={setDateTo} />
            </div>
            <div className="col-md-3">
              <button type="button" className="btn btn-primary w-100" onClick={applyFilter}>
                نمایش
              </button>
            </div>
          </div>

          {loading && (
            <div className="silo-empty-state">
              <p className="placeholder-text mb-0">در حال بارگذاری سود و زیان…</p>
            </div>
          )}

          {!loading && error && (
            <div className="silo-empty-state">
              <p className="text-danger mb-0">{error}</p>
            </div>
          )}

          {!loading && !error && data && (
            <>
              <div className="statement-kpi-grid mb-4">
                <article className="statement-kpi">
                  <span className="statement-kpi-label">درآمد (پایه)</span>
                  <AmountDisplay value={totals.revenue} />
                </article>
                <article className="statement-kpi">
                  <span className="statement-kpi-label">بهای تمام‌شده (پایه)</span>
                  <AmountDisplay value={totals.cogs} />
                </article>
                <article className="statement-kpi">
                  <span className="statement-kpi-label">سود ناخالص (پایه)</span>
                  <AmountDisplay value={totals.grossProfit} />
                </article>
                <article className="statement-kpi">
                  <span className="statement-kpi-label">هزینه (پایه)</span>
                  <AmountDisplay value={totals.expense} />
                </article>
                <article className={`statement-kpi is-highlight ${isProfit ? 'is-profit' : 'is-loss'}`}>
                  <span className="statement-kpi-label">{isProfit ? 'سود خالص (پایه)' : 'زیان خالص (پایه)'}</span>
                  <AmountDisplay value={Math.abs(netIncome)} />
                </article>
              </div>

              <CurrencyTotalsTable rows={data.byCurrency} mode="pl" />

              <div className="row g-3">
                <div className="col-lg-4">
                  <h4 className="statement-section-title">درآمدها</h4>
                  <StatementLineList rows={data.revenues} emptyText="درآمدی در این بازه ثبت نشده است." />
                </div>
                <div className="col-lg-4">
                  <h4 className="statement-section-title">بهای تمام‌شده</h4>
                  <StatementLineList rows={data.cogs} emptyText="بهای تمام‌شده‌ای ثبت نشده است." />
                </div>
                <div className="col-lg-4">
                  <h4 className="statement-section-title">هزینه‌ها</h4>
                  <StatementLineList rows={data.expenses} emptyText="هزینه‌ای در این بازه ثبت نشده است." />
                </div>
              </div>
            </>
          )}
        </div>
      </div>
    </section>
  )
}

function BalanceSheetCard() {
  const [asOf, setAsOf] = useState(todayGregorianIso())
  const [appliedAsOf, setAppliedAsOf] = useState(todayGregorianIso())
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let cancelled = false

    async function load() {
      setLoading(true)
      setError('')
      try {
        const result = await fetchBalanceSheet({
          asOf: toLatinIsoDate(appliedAsOf) || appliedAsOf,
        })
        if (!cancelled) setData(result)
      } catch (err) {
        if (!cancelled) {
          setData(null)
          setError(err.message || 'بارگذاری تراز کلی با خطا مواجه شد.')
        }
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    load()
    return () => {
      cancelled = true
    }
  }, [appliedAsOf])

  const totals = data?.totals
  const netIncome = Number(totals?.currentNetIncome ?? 0)

  return (
    <section className="mb-4">
      <div className="content-card card border-0">
        <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between flex-wrap gap-2">
          <div>
            <h3 className="card-title mb-1">تراز کلی شرکت</h3>
            <p className="silo-section-subtitle mb-0">
              دارایی‌ها، بدهی‌ها و حقوق مالکانه چندارزی؛ تراز معادله فقط روی ارز پایه
              {data?.asOfLabel ? ` — ${data.asOfLabel}` : ''}
            </p>
          </div>
          <Link to="/accounting/accounts" className="btn btn-sm btn-outline-accent">
            کدینگ حساب‌ها
          </Link>
        </div>

        <div className="card-body p-4">
          <div className="row g-3 align-items-end mb-4">
            <div className="col-md-3">
              <label className="form-label">تا تاریخ</label>
              <JalaliDateField value={asOf} onChange={setAsOf} />
            </div>
            <div className="col-md-3">
              <button
                type="button"
                className="btn btn-primary w-100"
                onClick={() => setAppliedAsOf(asOf)}
              >
                نمایش
              </button>
            </div>
          </div>

          {loading && (
            <div className="silo-empty-state">
              <p className="placeholder-text mb-0">در حال بارگذاری تراز کلی…</p>
            </div>
          )}

          {!loading && error && (
            <div className="silo-empty-state">
              <p className="text-danger mb-0">{error}</p>
            </div>
          )}

          {!loading && !error && data && (
            <>
              <div className="statement-kpi-grid mb-4">
                <article className="statement-kpi">
                  <span className="statement-kpi-label">جمع دارایی‌ها (پایه)</span>
                  <AmountDisplay value={totals.assets} />
                </article>
                <article className="statement-kpi">
                  <span className="statement-kpi-label">جمع بدهی‌ها (پایه)</span>
                  <AmountDisplay value={totals.liabilities} />
                </article>
                <article className="statement-kpi">
                  <span className="statement-kpi-label">حقوق مالکانه (پایه)</span>
                  <AmountDisplay value={totals.equity} />
                </article>
                <article className={`statement-kpi ${netIncome >= 0 ? 'is-profit' : 'is-loss'}`}>
                  <span className="statement-kpi-label">سود/زیان جاری (پایه)</span>
                  <AmountDisplay value={totals.currentNetIncome} />
                </article>
                <article className={`statement-kpi is-highlight ${totals.isBalanced ? 'is-balanced' : 'is-unbalanced'}`}>
                  <span className="statement-kpi-label">
                    {totals.isBalanced ? 'تراز متوازن (پایه)' : 'اختلاف تراز (پایه)'}
                  </span>
                  <AmountDisplay value={totals.isBalanced ? totals.liabilitiesAndEquity : totals.difference} />
                </article>
              </div>

              <CurrencyTotalsTable rows={data.byCurrency} mode="bs" />

              <div className="row g-3">
                <div className="col-lg-4">
                  <h4 className="statement-section-title">دارایی‌ها</h4>
                  <StatementLineList rows={data.assets} emptyText="مانده دارایی ثبت نشده است." />
                  <div className="statement-section-total">
                    <span>جمع معادل پایه</span>
                    <AmountDisplay value={totals.assets} />
                  </div>
                </div>
                <div className="col-lg-4">
                  <h4 className="statement-section-title">بدهی‌ها</h4>
                  <StatementLineList rows={data.liabilities} emptyText="مانده بدهی ثبت نشده است." />
                  <div className="statement-section-total">
                    <span>جمع معادل پایه</span>
                    <AmountDisplay value={totals.liabilities} />
                  </div>
                </div>
                <div className="col-lg-4">
                  <h4 className="statement-section-title">حقوق مالکانه</h4>
                  <StatementLineList rows={data.equity} emptyText="مانده حقوق مالکانه ثبت نشده است." />
                  {Math.abs(netIncome) >= 0.01 && (
                    <div className="statement-account-item statement-income-row">
                      <div className="statement-line-row">
                        <span className="statement-line-name">سود/زیان دوره جاری</span>
                        <AmountDisplay value={totals.currentNetIncome} />
                      </div>
                      <CurrencyAmountList currencies={data.currentNetByCurrency} />
                    </div>
                  )}
                  <div className="statement-section-total">
                    <span>جمع معادل پایه (با سود/زیان جاری)</span>
                    <AmountDisplay value={totals.equityWithIncome} />
                  </div>
                </div>
              </div>
            </>
          )}
        </div>
      </div>
    </section>
  )
}

function CashBoxesCard() {
  const [boxes, setBoxes] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let cancelled = false

    async function load() {
      setLoading(true)
      setError('')
      try {
        const data = await fetchCashBoxesOverview()
        if (!cancelled) setBoxes(Array.isArray(data) ? data : [])
      } catch (err) {
        if (!cancelled) {
          setBoxes([])
          setError(err.message || 'بارگذاری وضعیت صندوق‌ها با خطا مواجه شد.')
        }
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    load()
    return () => {
      cancelled = true
    }
  }, [])

  return (
    <section className="mb-4">
      <div className="content-card card border-0">
        <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between flex-wrap gap-2">
          <div>
            <h3 className="card-title mb-1">صندوق</h3>
            <p className="silo-section-subtitle mb-0">
              وضعیت صندوق‌ها و موجودی ارزها بر اساس اسناد دفترروزنامه
            </p>
          </div>
          <Link to="/cash/boxes" className="btn btn-sm btn-outline-accent">
            مدیریت صندوق‌ها
          </Link>
        </div>

        <div className="card-body p-4">
          {loading && (
            <div className="silo-empty-state">
              <p className="placeholder-text mb-0">در حال بارگذاری وضعیت صندوق‌ها…</p>
            </div>
          )}

          {!loading && error && (
            <div className="silo-empty-state">
              <p className="text-danger mb-0">{error}</p>
            </div>
          )}

          {!loading && !error && boxes.length === 0 && (
            <div className="silo-empty-state">
              <p className="placeholder-text mb-0">هنوز صندوقی ثبت نشده است.</p>
            </div>
          )}

          {!loading && !error && boxes.length > 0 && (
            <div className="cash-overview-grid">
              {boxes.map((box) => (
                <article
                  key={box.cashBoxId}
                  className={`cash-overview-item${!box.isActive ? ' is-inactive' : ''}`}
                >
                  <div className="cash-overview-head">
                    <div className="d-flex align-items-start gap-2">
                      <div className="stat-icon cash-overview-icon">
                        <Icon name="cash" />
                      </div>
                      <div>
                        <h4 className="cash-overview-name mb-0">{box.name}</h4>
                        <p className="cash-overview-code mb-0">{box.code}</p>
                      </div>
                    </div>
                    <div className="cash-overview-badges">
                      <span
                        className={`badge-status cash-status ${box.isActive ? 'is-active' : 'is-inactive'}`}
                      >
                        {box.isActive ? 'فعال' : 'غیرفعال'}
                      </span>
                      <span
                        className={`badge-status cash-status ${box.hasOpenShift ? 'is-open' : 'is-closed'}`}
                      >
                        {box.hasOpenShift ? 'شیفت باز' : 'بدون شیفت'}
                      </span>
                    </div>
                  </div>

                  {(box.parentName || box.openShiftUserName) && (
                    <div className="cash-overview-meta">
                      {box.parentName ? <span>صندوق بالاتر: {box.parentName}</span> : null}
                      {box.hasOpenShift && box.openShiftUserName ? (
                        <span>کاربر شیفت: {box.openShiftUserName}</span>
                      ) : null}
                    </div>
                  )}

                  <div className="cash-overview-balances">
                    {(box.balances ?? []).length === 0 ? (
                      <p className="cash-overview-empty mb-0">موجودی صفر</p>
                    ) : (
                      <ul className="cash-balance-list">
                        {(box.balances ?? []).map((b) => (
                          <li key={b.currencyId} className="cash-balance-row">
                            <span className="cash-balance-currency">
                              {b.currencyCode || b.symbol || b.name}
                              {b.isBaseCurrency ? (
                                <span className="cash-balance-base">پایه</span>
                              ) : null}
                            </span>
                            <AmountDisplay value={b.amount} symbol={b.symbol || b.currencyCode} />
                          </li>
                        ))}
                      </ul>
                    )}
                  </div>

                  <div className="cash-overview-total">
                    <span>معادل پایه</span>
                    <AmountDisplay value={box.totalInBase} />
                  </div>
                </article>
              ))}
            </div>
          )}
        </div>
      </div>
    </section>
  )
}

function ReportsPage() {
  return (
    <div className="reports-page">
      <section className="mb-4">
        <div className="page-welcome card border-0">
          <div className="card-body p-4">
            <h2 className="welcome-title mb-2">آمار و تحلیل</h2>
            <p className="welcome-text mb-0">
              نمای کلی وضعیت مالی شرکت: سود و زیان، تراز کلی و موجودی صندوق‌ها.
            </p>
          </div>
        </div>
      </section>

      <ProfitLossCard />
      <BalanceSheetCard />
      <CashBoxesCard />
    </div>
  )
}

export default ReportsPage
