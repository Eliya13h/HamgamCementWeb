import { useCallback, useEffect, useMemo, useState } from 'react'
import AmountField from '../../components/common/AmountField'
import JalaliDateField from '../../components/common/JalaliDateField'
import Icon from '../../components/common/Icon'
import {
  afghanSolarLocale,
  currentJalaliYearMonth,
  formatJalaliDate,
  getJalaliMonthRange,
  todayGregorianIso,
} from '../../lib/afghanSolarCalendar'
import { formatAmount } from '../../lib/dataTableOptions'
import { showAppToast } from '../../lib/appToast'
import { fetchBaseCurrency } from '../../services/currenciesApi'
import {
  createSalaryPayment,
  deleteSalaryPayment,
  fetchAttendanceMonth,
  fetchSalaryCashBoxOptions,
  fetchSalaryPayments,
  fetchSalaryPreview,
} from '../../services/hrApi'

function SalaryPaymentsPage() {
  const now = useMemo(() => currentJalaliYearMonth(), [])
  const [year, setYear] = useState(now.year)
  const [month, setMonth] = useState(now.month)
  const [list, setList] = useState([])
  const [employees, setEmployees] = useState([])
  const [cashBoxes, setCashBoxes] = useState([])
  const [baseCurrencySymbol, setBaseCurrencySymbol] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [deletingId, setDeletingId] = useState(null)

  const [employeeId, setEmployeeId] = useState('')
  const [paymentDate, setPaymentDate] = useState(todayGregorianIso())
  const [cashBoxId, setCashBoxId] = useState('')
  const [description, setDescription] = useState('')

  const [preview, setPreview] = useState(null)
  const [baseSalary, setBaseSalary] = useState('0')
  const [overtimeAmount, setOvertimeAmount] = useState('0')
  const [lateDeduction, setLateDeduction] = useState('0')
  const [absenceDeduction, setAbsenceDeduction] = useState('0')
  const [benefitAmount, setBenefitAmount] = useState('0')
  const [otherDeduction, setOtherDeduction] = useState('0')

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
    try {
      const rows = await fetchSalaryPayments({ year, month })
      setList(rows ?? [])
    } catch (err) {
      showAppToast(err.message || 'بارگذاری فیش‌های حقوق با خطا مواجه شد.')
      setList([])
    } finally {
      setLoading(false)
    }
  }, [year, month])

  useEffect(() => {
    let cancelled = false

    void fetchSalaryPayments({ year, month })
      .then((rows) => {
        if (cancelled) return
        setList(rows ?? [])
      })
      .catch((err) => {
        if (cancelled) return
        showAppToast(err.message || 'بارگذاری فیش‌های حقوق با خطا مواجه شد.')
        setList([])
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [year, month])

  useEffect(() => {
    let cancelled = false

    void fetchAttendanceMonth(year, month)
      .then((data) => {
        if (cancelled) return
        setEmployees(
          (data.rows ?? []).map((r) => ({
            employeeId: r.employeeId,
            fullName: r.fullName,
          })),
        )
      })
      .catch((err) => {
        if (cancelled) return
        setEmployees([])
        showAppToast(err.message || 'بارگذاری لیست کارمندان با خطا مواجه شد.')
      })

    void fetchSalaryCashBoxOptions()
      .then((rows) => {
        if (cancelled) return
        const mapped = (rows ?? []).map((r) => ({
          value: String(r.value),
          label: r.label,
        }))
        setCashBoxes(mapped)
        setCashBoxId((prev) => {
          if (prev && mapped.some((b) => b.value === prev)) return prev
          return mapped.length === 1 ? mapped[0].value : prev
        })
      })
      .catch((err) => {
        if (cancelled) return
        setCashBoxes([])
        showAppToast(err.message || 'بارگذاری صندوق‌ها با خطا مواجه شد.')
      })

    return () => {
      cancelled = true
    }
  }, [year, month])

  useEffect(() => {
    let cancelled = false
    void fetchBaseCurrency()
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

  const loadPreview = async () => {
    setPreview(null)
    if (!employeeId) {
      showAppToast('کارمند را انتخاب کنید.', 'warning')
      return
    }
    if (!range.from || !range.to) {
      showAppToast('بازه ماه معتبر نیست.', 'warning')
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
      setBaseSalary(String(data.baseSalary ?? 0))
      setOvertimeAmount(String(data.suggestedOvertimeAmount ?? 0))
      setLateDeduction(String(data.suggestedLateDeduction ?? 0))
      setAbsenceDeduction(String(data.suggestedAbsenceDeduction ?? 0))
      setBenefitAmount(String(data.suggestedBenefitAmount ?? 0))
      setOtherDeduction(String(data.suggestedOtherDeduction ?? 0))
      if (data.hasAttendanceSummary === false) {
        showAppToast(
          'خلاصه حضور این ماه ثبت نشده است؛ مبالغ پیشنهادی صفر هستند.',
          'warning',
        )
      } else {
        showAppToast('پیش‌نویس حقوق آماده شد.', 'success')
      }
    } catch (err) {
      showAppToast(err.message || 'دریافت پیش‌نویس حقوق با خطا مواجه شد.')
    }
  }

  const onDelete = async (row) => {
    if (!window.confirm(`آیا از حذف فیش حقوق «${row.employeeName}» اطمینان دارید؟`)) {
      return
    }
    setDeletingId(row.salaryPaymentId)
    try {
      const result = await deleteSalaryPayment(row.salaryPaymentId)
      showAppToast(result.message || 'پرداخت حقوق حذف شد.', 'success')
      await loadList()
    } catch (err) {
      showAppToast(err.message || 'حذف فیش حقوق با خطا مواجه شد.')
    } finally {
      setDeletingId(null)
    }
  }

  const onCreate = async (event) => {
    event.preventDefault()
    if (!preview) {
      showAppToast('ابتدا پیش‌نویس را بارگذاری کنید.', 'warning')
      return
    }
    if (netAmount <= 0) {
      showAppToast('مبلغ خالص پرداختی باید بیشتر از صفر باشد.', 'warning')
      return
    }
    if (!cashBoxId) {
      showAppToast('صندوق پرداخت را انتخاب کنید.', 'warning')
      return
    }

    setSaving(true)
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
        cashBoxId: Number(cashBoxId),
        description: description || null,
      })
      showAppToast(result.message || 'حقوق با موفقیت ثبت شد.', 'success')
      setPreview(null)
      setEmployeeId('')
      setDescription('')
      await loadList()
    } catch (err) {
      showAppToast(err.message || 'ثبت حقوق با خطا مواجه شد.')
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
            از روی خلاصه حضور ماهانه، مبلغ پیشنهادی ساخته می‌شود؛ کسورات و اضافه‌کاری را
            دستی اصلاح کنید.
          </p>

          <div className="row g-2 mb-4 align-items-end">
            <div className="col-auto">
              <label className="form-label mb-1">سال شمسی</label>
              <input
                type="number"
                className="form-control"
                style={{ width: 110 }}
                value={year}
                onChange={(e) => {
                  setLoading(true)
                  setYear(Number(e.target.value) || now.year)
                }}
              />
            </div>
            <div className="col-auto">
              <label className="form-label mb-1">ماه</label>
              <select
                className="form-select"
                value={month}
                onChange={(e) => {
                  setLoading(true)
                  setMonth(Number(e.target.value))
                }}
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
              <div className="col-md-4">
                <label className="form-label">
                  صندوق پرداخت <span className="text-danger">*</span>
                </label>
                <select
                  className="form-select"
                  value={cashBoxId}
                  onChange={(e) => setCashBoxId(e.target.value)}
                  required
                >
                  <option value="">انتخاب صندوق پرداخت</option>
                  {cashBoxes.map((b) => (
                    <option key={b.value} value={b.value}>
                      {b.label}
                    </option>
                  ))}
                </select>
                {cashBoxes.length === 0 && (
                  <div className="form-text text-danger">
                    صندوق فعالی یافت نشد. ابتدا از بخش صندوق‌ها یک صندوق بسازید.
                  </div>
                )}
              </div>
              <div className="col-md-3">
                <label className="form-label">تاریخ پرداخت</label>
                <JalaliDateField
                  value={paymentDate}
                  onChange={setPaymentDate}
                  required
                />
              </div>
              <div className="col-md-3 d-flex align-items-end">
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
                      حاضر: {preview.presentDays} روز — غایب برای کسر:{' '}
                      {preview.absentForDeduction ?? preview.absentDays} روز —
                      تأخیر: {preview.lateHours ?? 0} ساعت — اضافه‌کار:{' '}
                      {preview.overtimeHours ?? 0} ساعت (ضریب{' '}
                      {preview.overtimeCoefficient ?? 1.5})
                      {preview.hasAttendanceSummary === false && (
                        <span className="text-warning d-block mt-1">
                          خلاصه حضور این ماه ثبت نشده است.
                        </span>
                      )}
                    </div>
                  </div>

                  <div className="col-md-4">
                    <label className="form-label">حقوق پایه</label>
                    <AmountField
                      symbol={baseCurrencySymbol}
                      step={100}
                      min={0}
                      value={baseSalary}
                      onChange={setBaseSalary}
                    />
                  </div>
                  <div className="col-md-4">
                    <label className="form-label">مبلغ اضافه‌کاری</label>
                    <AmountField
                      symbol={baseCurrencySymbol}
                      step={100}
                      min={0}
                      value={overtimeAmount}
                      onChange={setOvertimeAmount}
                    />
                  </div>
                  <div className="col-md-4">
                    <label className="form-label">مزایا</label>
                    <AmountField
                      symbol={baseCurrencySymbol}
                      step={100}
                      min={0}
                      value={benefitAmount}
                      onChange={setBenefitAmount}
                    />
                  </div>
                  <div className="col-md-4">
                    <label className="form-label">کسر دیرکرد</label>
                    <AmountField
                      symbol={baseCurrencySymbol}
                      step={100}
                      min={0}
                      value={lateDeduction}
                      onChange={setLateDeduction}
                    />
                  </div>
                  <div className="col-md-4">
                    <label className="form-label">کسر غیبت</label>
                    <AmountField
                      symbol={baseCurrencySymbol}
                      step={100}
                      min={0}
                      value={absenceDeduction}
                      onChange={setAbsenceDeduction}
                    />
                  </div>
                  <div className="col-md-4">
                    <label className="form-label">سایر کسورات</label>
                    <AmountField
                      symbol={baseCurrencySymbol}
                      step={100}
                      min={0}
                      value={otherDeduction}
                      onChange={setOtherDeduction}
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
                    <AmountField
                      symbol={baseCurrencySymbol}
                      value={netAmount}
                      onChange={() => {}}
                      readOnly
                    />
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
                  <th>صندوق</th>
                  <th className="text-end">پایه</th>
                  <th className="text-end">اضافه‌کار</th>
                  <th className="text-end">کسورات</th>
                  <th className="text-end">مزایا</th>
                  <th className="text-end">خالص</th>
                  <th>سند</th>
                  <th className="text-center">عملیات</th>
                </tr>
              </thead>
              <tbody>
                {loading && (
                  <tr>
                    <td colSpan={10} className="text-center text-muted py-3">
                      در حال بارگذاری...
                    </td>
                  </tr>
                )}
                {!loading && list.length === 0 && (
                  <tr>
                    <td colSpan={10} className="text-center text-muted py-3">
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
                        <td>{row.cashBoxName || '—'}</td>
                        <td className="text-end">
                          <span
                            className="amount-cell"
                            dir="ltr"
                            {...(baseCurrencySymbol
                              ? { 'data-currency': baseCurrencySymbol }
                              : {})}
                          >
                            {formatAmount(row.baseSalary)}
                          </span>
                        </td>
                        <td className="text-end">
                          <span
                            className="amount-cell"
                            dir="ltr"
                            {...(baseCurrencySymbol
                              ? { 'data-currency': baseCurrencySymbol }
                              : {})}
                          >
                            {formatAmount(row.overtimeAmount)}
                          </span>
                        </td>
                        <td className="text-end">
                          <span
                            className="amount-cell"
                            dir="ltr"
                            {...(baseCurrencySymbol
                              ? { 'data-currency': baseCurrencySymbol }
                              : {})}
                          >
                            {formatAmount(deductions)}
                          </span>
                        </td>
                        <td className="text-end">
                          <span
                            className="amount-cell"
                            dir="ltr"
                            {...(baseCurrencySymbol
                              ? { 'data-currency': baseCurrencySymbol }
                              : {})}
                          >
                            {formatAmount(row.benefitAmount)}
                          </span>
                        </td>
                        <td className="text-end fw-semibold">
                          <span
                            className="amount-cell"
                            dir="ltr"
                            {...(baseCurrencySymbol
                              ? { 'data-currency': baseCurrencySymbol }
                              : {})}
                          >
                            {formatAmount(row.netAmount)}
                          </span>
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
                        <td className="text-center">
                          <button
                            type="button"
                            className="btn btn-sm btn-outline-danger"
                            title="حذف"
                            disabled={deletingId === row.salaryPaymentId}
                            onClick={() => onDelete(row)}
                          >
                            <Icon name="trash" size={14} />
                          </button>
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
