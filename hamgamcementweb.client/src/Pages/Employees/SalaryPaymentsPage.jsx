import { useCallback, useEffect, useMemo, useState } from 'react'
import JalaliDateField from '../../components/common/JalaliDateField'
import {
  afghanSolarLocale,
  currentJalaliYearMonth,
  formatJalaliDate,
  getJalaliMonthRange,
  todayGregorianIso,
} from '../../lib/afghanSolarCalendar'
import { fetchCashBoxOptions } from '../../services/ledgerApi'
import {
  createSalaryPayment,
  fetchAttendanceRange,
  fetchSalaryPayments,
  fetchSalaryPreview,
} from '../../services/hrApi'

function formatMoney(value) {
  const n = Number(value)
  if (!Number.isFinite(n)) return '—'
  return n.toLocaleString('en-US', { maximumFractionDigits: 2 })
}

function SalaryPaymentsPage() {
  const now = useMemo(() => currentJalaliYearMonth(), [])
  const [year, setYear] = useState(now.year)
  const [month, setMonth] = useState(now.month)
  const [list, setList] = useState([])
  const [employees, setEmployees] = useState([])
  const [cashBoxes, setCashBoxes] = useState([])
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')

  const [employeeId, setEmployeeId] = useState('')
  const [paymentDate, setPaymentDate] = useState(todayGregorianIso())
  const [cashBoxId, setCashBoxId] = useState('')
  const [description, setDescription] = useState('')

  const [preview, setPreview] = useState(null)
  const [baseSalary, setBaseSalary] = useState(0)
  const [overtimeAmount, setOvertimeAmount] = useState(0)
  const [lateDeduction, setLateDeduction] = useState(0)
  const [absenceDeduction, setAbsenceDeduction] = useState(0)
  const [benefitAmount, setBenefitAmount] = useState(0)
  const [otherDeduction, setOtherDeduction] = useState(0)

  const range = useMemo(
    () => getJalaliMonthRange(year, month),
    [year, month],
  )

  const netAmount = useMemo(() => {
    const net =
      Number(baseSalary || 0) +
      Number(overtimeAmount || 0) +
      Number(benefitAmount || 0) -
      Number(lateDeduction || 0) -
      Number(absenceDeduction || 0) -
      Number(otherDeduction || 0)
    return Math.round(net * 10000) / 10000
  }, [
    baseSalary,
    overtimeAmount,
    benefitAmount,
    lateDeduction,
    absenceDeduction,
    otherDeduction,
  ])

  const loadList = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const rows = await fetchSalaryPayments({ year, month })
      setList(rows ?? [])
    } catch (err) {
      setError(err.message)
      setList([])
    } finally {
      setLoading(false)
    }
  }, [year, month])

  useEffect(() => {
    loadList()
  }, [loadList])

  useEffect(() => {
    fetchAttendanceRange(range.from, range.to)
      .then((data) => {
        setEmployees(data.employees ?? [])
      })
      .catch(() => setEmployees([]))

    fetchCashBoxOptions()
      .then((rows) =>
        setCashBoxes(
          (rows ?? []).map((r) => ({
            value: String(r.value),
            label: r.label,
          })),
        ),
      )
      .catch(() => setCashBoxes([]))
  }, [range.from, range.to])

  const loadPreview = async () => {
    setError('')
    setMessage('')
    setPreview(null)
    if (!employeeId) {
      setError('کارمند را انتخاب کنید.')
      return
    }
    if (!range.from || !range.to) {
      setError('بازه ماه معتبر نیست.')
      return
    }

    try {
      const data = await fetchSalaryPreview({
        employeeId: Number(employeeId),
        year: Number(year),
        month: Number(month),
        from: range.from,
        to: range.to,
      })
      setPreview(data)
      setBaseSalary(data.baseSalary ?? 0)
      setOvertimeAmount(data.suggestedOvertimeAmount ?? 0)
      setLateDeduction(data.suggestedLateDeduction ?? 0)
      setAbsenceDeduction(data.suggestedAbsenceDeduction ?? 0)
      setBenefitAmount(data.suggestedBenefitAmount ?? 0)
      setOtherDeduction(data.suggestedOtherDeduction ?? 0)
    } catch (err) {
      setError(err.message)
    }
  }

  const onCreate = async (event) => {
    event.preventDefault()
    if (!preview) {
      setError('ابتدا پیش‌نویس را بارگذاری کنید.')
      return
    }

    setSaving(true)
    setError('')
    setMessage('')
    try {
      const result = await createSalaryPayment({
        employeeId: Number(employeeId),
        year: Number(year),
        month: Number(month),
        paymentDate,
        baseSalary: Number(baseSalary) || 0,
        overtimeAmount: Number(overtimeAmount) || 0,
        lateDeduction: Number(lateDeduction) || 0,
        absenceDeduction: Number(absenceDeduction) || 0,
        benefitAmount: Number(benefitAmount) || 0,
        otherDeduction: Number(otherDeduction) || 0,
        presentDays: preview.presentDays,
        absentDays: preview.absentDays,
        totalLateMinutes: preview.totalLateMinutes,
        totalOvertimeMinutes: preview.totalOvertimeMinutes,
        cashBoxId: cashBoxId ? Number(cashBoxId) : null,
        description: description || null,
      })
      setMessage(result.message)
      setPreview(null)
      setEmployeeId('')
      await loadList()
    } catch (err) {
      setError(err.message)
    } finally {
      setSaving(false)
    }
  }

  const monthOptions = afghanSolarLocale.months.map((m, index) => ({
    value: index + 1,
    label: m[0],
  }))

  return (
    <div className="users-page">
      <div className="content-card card border-0 mb-3">
        <div className="card-body p-4">
          <h2 className="card-title mb-1">حقوق و مزایا</h2>
          <p className="text-muted small mb-3">
            از روی حضور ماه، مبلغ پیشنهادی ساخته می‌شود؛ کسورات و اضافه‌کاری را دستی اصلاح کنید.
          </p>

          {message && <div className="alert alert-success py-2">{message}</div>}
          {error && <div className="alert alert-danger py-2">{error}</div>}

          <div className="row g-2 mb-4 align-items-end">
            <div className="col-auto">
              <label className="form-label mb-1">سال شمسی</label>
              <input
                type="number"
                className="form-control"
                style={{ width: 110 }}
                value={year}
                onChange={(e) => setYear(Number(e.target.value) || now.year)}
              />
            </div>
            <div className="col-auto">
              <label className="form-label mb-1">ماه</label>
              <select
                className="form-select"
                value={month}
                onChange={(e) => setMonth(Number(e.target.value))}
              >
                {monthOptions.map((m) => (
                  <option key={m.value} value={m.value}>
                    {m.label}
                  </option>
                ))}
              </select>
            </div>
            <div className="col-auto text-muted small pb-2">
              بازه: {formatJalaliDate(range.from)} تا {formatJalaliDate(range.to)}
            </div>
          </div>

          <form onSubmit={onCreate} className="border rounded p-3 mb-4">
            <h3 className="h6 mb-3">ثبت حقوق ماه</h3>
            <div className="row g-3">
              <div className="col-md-4">
                <label className="form-label">کارمند</label>
                <select
                  className="form-select"
                  value={employeeId}
                  onChange={(e) => {
                    setEmployeeId(e.target.value)
                    setPreview(null)
                  }}
                  required
                >
                  <option value="">انتخاب کنید</option>
                  {employees.map((emp) => (
                    <option key={emp.employeeId} value={emp.employeeId}>
                      {emp.fullName}
                    </option>
                  ))}
                </select>
              </div>
              <div className="col-md-3">
                <label className="form-label">تاریخ پرداخت</label>
                <JalaliDateField
                  value={paymentDate}
                  onChange={setPaymentDate}
                  required
                />
              </div>
              <div className="col-md-3">
                <label className="form-label">صندوق (اختیاری)</label>
                <select
                  className="form-select"
                  value={cashBoxId}
                  onChange={(e) => setCashBoxId(e.target.value)}
                >
                  <option value="">پیش‌فرض کاربر / بانک</option>
                  {cashBoxes.map((b) => (
                    <option key={b.value} value={b.value}>
                      {b.label}
                    </option>
                  ))}
                </select>
              </div>
              <div className="col-md-2 d-flex align-items-end">
                <button
                  type="button"
                  className="btn btn-outline-primary w-100"
                  onClick={loadPreview}
                >
                  پیش‌نویس
                </button>
              </div>

              {preview && (
                <>
                  <div className="col-12">
                    <div className="alert alert-light border py-2 mb-0 small">
                      حاضر: {preview.presentDays} روز — غایب (پیشنهادی):{' '}
                      {preview.absentDays} روز — دیرکرد: {preview.totalLateMinutes}{' '}
                      دقیقه — اضافه‌کار: {preview.totalOvertimeMinutes} دقیقه
                    </div>
                  </div>

                  <div className="col-md-4">
                    <label className="form-label">حقوق پایه</label>
                    <input
                      type="number"
                      className="form-control"
                      value={baseSalary}
                      onChange={(e) => setBaseSalary(e.target.value)}
                    />
                  </div>
                  <div className="col-md-4">
                    <label className="form-label">مبلغ اضافه‌کاری</label>
                    <input
                      type="number"
                      className="form-control"
                      value={overtimeAmount}
                      onChange={(e) => setOvertimeAmount(e.target.value)}
                    />
                  </div>
                  <div className="col-md-4">
                    <label className="form-label">مزایا</label>
                    <input
                      type="number"
                      className="form-control"
                      value={benefitAmount}
                      onChange={(e) => setBenefitAmount(e.target.value)}
                    />
                  </div>
                  <div className="col-md-4">
                    <label className="form-label">کسر دیرکرد</label>
                    <input
                      type="number"
                      className="form-control"
                      value={lateDeduction}
                      onChange={(e) => setLateDeduction(e.target.value)}
                    />
                  </div>
                  <div className="col-md-4">
                    <label className="form-label">کسر غیبت</label>
                    <input
                      type="number"
                      className="form-control"
                      value={absenceDeduction}
                      onChange={(e) => setAbsenceDeduction(e.target.value)}
                    />
                  </div>
                  <div className="col-md-4">
                    <label className="form-label">سایر کسورات</label>
                    <input
                      type="number"
                      className="form-control"
                      value={otherDeduction}
                      onChange={(e) => setOtherDeduction(e.target.value)}
                    />
                  </div>
                  <div className="col-md-8">
                    <label className="form-label">توضیحات</label>
                    <input
                      type="text"
                      className="form-control"
                      value={description}
                      onChange={(e) => setDescription(e.target.value)}
                    />
                  </div>
                  <div className="col-md-4">
                    <label className="form-label">خالص پرداختی</label>
                    <div className="form-control bg-light fw-semibold">
                      {formatMoney(netAmount)}
                    </div>
                  </div>
                  <div className="col-12">
                    <button
                      type="submit"
                      className="btn btn-primary"
                      disabled={saving || netAmount <= 0}
                    >
                      {saving ? 'در حال ثبت...' : 'ثبت حقوق و سند حسابداری'}
                    </button>
                  </div>
                </>
              )}
            </div>
          </form>

          <h3 className="h6 mb-2">فیش‌های ثبت‌شده این ماه</h3>
          <div className="table-responsive">
            <table className="table table-sm table-hover align-middle">
              <thead>
                <tr>
                  <th>کارمند</th>
                  <th>تاریخ پرداخت</th>
                  <th className="text-end">پایه</th>
                  <th className="text-end">اضافه‌کار</th>
                  <th className="text-end">کسورات</th>
                  <th className="text-end">مزایا</th>
                  <th className="text-end">خالص</th>
                  <th>سند</th>
                </tr>
              </thead>
              <tbody>
                {loading && (
                  <tr>
                    <td colSpan={8} className="text-center text-muted py-3">
                      در حال بارگذاری...
                    </td>
                  </tr>
                )}
                {!loading && list.length === 0 && (
                  <tr>
                    <td colSpan={8} className="text-center text-muted py-3">
                      فیشی برای این ماه ثبت نشده است.
                    </td>
                  </tr>
                )}
                {!loading &&
                  list.map((row) => {
                    const deductions =
                      Number(row.lateDeduction || 0) +
                      Number(row.absenceDeduction || 0) +
                      Number(row.otherDeduction || 0)
                    return (
                      <tr key={row.salaryPaymentId}>
                        <td>{row.employeeName}</td>
                        <td>{formatJalaliDate(row.paymentDate)}</td>
                        <td className="text-end">{formatMoney(row.baseSalary)}</td>
                        <td className="text-end">
                          {formatMoney(row.overtimeAmount)}
                        </td>
                        <td className="text-end">{formatMoney(deductions)}</td>
                        <td className="text-end">
                          {formatMoney(row.benefitAmount)}
                        </td>
                        <td className="text-end fw-semibold">
                          {formatMoney(row.netAmount)}
                        </td>
                        <td>
                          {row.journalEntryId ? (
                            <span className="badge bg-secondary">
                              #{row.journalEntryId}
                            </span>
                          ) : (
                            '—'
                          )}
                        </td>
                      </tr>
                    )
                  })}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  )
}

export default SalaryPaymentsPage
