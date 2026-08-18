import { useEffect, useMemo, useState } from 'react'
import JalaliDateField from '../../components/common/JalaliDateField'
import { currentJalaliYearMonth, getJalaliYearRange, toLatinIsoDate, todayGregorianIso } from '../../lib/afghanSolarCalendar'
import { formatAmount, formatJalaliDate } from '../../components/common/CrudTablePage'
import { fetchAccountLedger, fetchAccountTree } from '../../services/ledgerApi'
import { getLedgerReportUrl } from '../../services/journalApi'

function LedgerPage() {
  const yearStart = useMemo(() => {
    const { year } = currentJalaliYearMonth()
    return getJalaliYearRange(year).from
  }, [])

  const [accounts, setAccounts] = useState([])
  const [accountId, setAccountId] = useState('')
  const [dateFrom, setDateFrom] = useState(yearStart)
  const [dateTo, setDateTo] = useState(todayGregorianIso())
  const [partyId, setPartyId] = useState('')
  const [data, setData] = useState(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [accountsError, setAccountsError] = useState('')

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        const tree = await fetchAccountTree()
        if (cancelled) return
        const list = (Array.isArray(tree) ? tree : [])
          .filter((a) => a.isPostable === true)
          .map((a) => ({
            accountId: a.accountId,
            code: a.code,
            name: a.name,
            label: `${a.code} — ${a.name}`,
          }))
          .sort((a, b) => String(a.code).localeCompare(String(b.code), 'fa'))
        setAccounts(list)
        setAccountsError('')
      } catch (err) {
        if (!cancelled) {
          setAccountsError(err.message || 'بارگذاری حساب‌ها ناموفق بود.')
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  const loadLedger = async () => {
    setError('')
    if (!accountId) {
      setError('لطفاً یک حساب انتخاب کنید.')
      return
    }
    if (dateFrom && dateTo && dateFrom > dateTo) {
      setError('تاریخ شروع نباید بعد از تاریخ پایان باشد.')
      return
    }

    setLoading(true)
    try {
      const result = await fetchAccountLedger(Number(accountId), {
        dateFrom: toLatinIsoDate(dateFrom) || undefined,
        dateTo: toLatinIsoDate(dateTo) || undefined,
        partyId: partyId ? Number(partyId) : undefined,
      })
      setData(result)
    } catch (err) {
      setData(null)
      setError(err.message || 'بارگذاری دفتر کل ناموفق بود.')
    } finally {
      setLoading(false)
    }
  }

  const openPrint = () => {
    setError('')
    if (!accountId) {
      setError('لطفاً یک حساب انتخاب کنید.')
      return
    }
    if (dateFrom && dateTo && dateFrom > dateTo) {
      setError('تاریخ شروع نباید بعد از تاریخ پایان باشد.')
      return
    }
    window.open(
      getLedgerReportUrl(
        Number(accountId),
        dateFrom,
        dateTo,
        partyId ? Number(partyId) : undefined,
      ),
      '_blank',
      'noopener,noreferrer',
    )
  }

  const lines = data?.lines ?? []

  return (
    <div className="content-card card border-0">
      <div className="card-body p-4">
        <h2 className="card-title mb-2">دفتر کل</h2>
        <p className="text-muted mb-4">
          گردش یک حساب با مانده افتتاحیه، دیبت/کریدیت دوره و مانده جاری. برای چاپ از دکمه چاپ استفاده کنید.
        </p>

        {(error || accountsError) && (
          <div className="alert alert-danger py-2 mb-3">{error || accountsError}</div>
        )}

        <div className="row g-3 align-items-end mb-3">
          <div className="col-md-4">
            <label className="form-label" htmlFor="ledger-account">
              حساب
            </label>
            <select
              id="ledger-account"
              className="form-select"
              value={accountId}
              onChange={(e) => setAccountId(e.target.value)}
            >
              <option value="">انتخاب حساب...</option>
              {accounts.map((a) => (
                <option key={a.accountId} value={a.accountId}>
                  {a.label}
                </option>
              ))}
            </select>
          </div>
          <div className="col-md-2">
            <label className="form-label">از تاریخ</label>
            <JalaliDateField value={dateFrom} onChange={setDateFrom} />
          </div>
          <div className="col-md-2">
            <label className="form-label">تا تاریخ</label>
            <JalaliDateField value={dateTo} onChange={setDateTo} />
          </div>
          <div className="col-md-2">
            <label className="form-label" htmlFor="ledger-party">
              طرف‌حساب (اختیاری)
            </label>
            <input
              id="ledger-party"
              className="form-control"
              type="number"
              min="1"
              placeholder="شناسه"
              value={partyId}
              onChange={(e) => setPartyId(e.target.value)}
            />
          </div>
          <div className="col-md-2 d-flex gap-2">
            <button type="button" className="btn btn-primary flex-grow-1" onClick={loadLedger} disabled={loading}>
              {loading ? '...' : 'نمایش'}
            </button>
            <button type="button" className="btn btn-outline-secondary" onClick={openPrint}>
              چاپ
            </button>
          </div>
        </div>

        {data && (
          <>
            <div className="row g-2 mb-3 small">
              <div className="col-md-3">
                <div className="border rounded p-2">
                  حساب: <span className="font-monospace">{data.code}</span> {data.name}
                </div>
              </div>
              <div className="col-md-3">
                <div className="border rounded p-2">
                  مانده افتتاحیه: {formatAmount(data.openingBalance)}
                </div>
              </div>
              <div className="col-md-3">
                <div className="border rounded p-2">
                  بازه: {data.fromLabel || data.from} تا {data.toLabel || data.to}
                </div>
              </div>
              <div className="col-md-3">
                <div className="border rounded p-2">
                  مانده اختتامیه: {formatAmount(data.closingBalance)}
                </div>
              </div>
            </div>

            <div className="table-responsive">
              <table className="table table-sm table-striped align-middle">
                <thead>
                  <tr>
                    <th>تاریخ</th>
                    <th>شماره سند</th>
                    <th>شرح</th>
                    <th className="text-end">دیبت (Db)</th>
                    <th className="text-end">کریدیت (Cr)</th>
                    <th className="text-end">مانده</th>
                  </tr>
                </thead>
                <tbody>
                  {lines.length === 0 ? (
                    <tr>
                      <td colSpan={6} className="text-center text-muted py-4">
                        در این بازه گردشی ثبت نشده است.
                      </td>
                    </tr>
                  ) : (
                    lines.map((line) => (
                      <tr key={line.journalLineId ?? `${line.journalEntryId}-${line.lineNo}`}>
                        <td>{formatJalaliDate(line.entryDate)}</td>
                        <td className="font-monospace">{line.entryNumber}</td>
                        <td>{line.lineDescription || line.entryDescription || '—'}</td>
                        <td className="text-end">
                          {line.debitInBase ? formatAmount(line.debitInBase) : ''}
                        </td>
                        <td className="text-end">
                          {line.creditInBase ? formatAmount(line.creditInBase) : ''}
                        </td>
                        <td className="text-end">{formatAmount(line.runningBalance)}</td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </>
        )}
      </div>
    </div>
  )
}

export default LedgerPage
