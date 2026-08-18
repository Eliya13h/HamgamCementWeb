import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import {
  afghanSolarLocale,
  countJalaliMonthDaysUntilToday,
  currentJalaliYearMonth,
} from '../../lib/afghanSolarCalendar'
import { showAppToast } from '../../lib/appToast'
import {
  fetchAttendanceMonth,
  upsertAttendanceMonth,
} from '../../services/hrApi'

const DEFAULT_OVERTIME_COEFFICIENT = 1.5

function mapApiRow(row, defaults) {
  const isSaved = !!row.isSaved
  return {
    employeeId: row.employeeId,
    fullName: row.fullName,
    departmentName: row.departmentName ?? '',
    year: row.year,
    month: row.month,
    presentDays: isSaved ? (row.presentDays ?? 0) : defaults.presentDays,
    absentDays: row.absentDays ?? 0,
    leavePaidDays: row.leavePaidDays ?? 0,
    leaveUnpaidDays: row.leaveUnpaidDays ?? 0,
    holidayPaidDays: isSaved
      ? (row.holidayPaidDays ?? 0)
      : defaults.holidayPaidDays,
    holidayUnpaidDays: row.holidayUnpaidDays ?? 0,
    lateHours: row.lateHours ?? 0,
    earlyLeaveHours: row.earlyLeaveHours ?? 0,
    overtimeHours: row.overtimeHours ?? 0,
    overtimeCoefficient:
      row.overtimeCoefficient > 0
        ? row.overtimeCoefficient
        : DEFAULT_OVERTIME_COEFFICIENT,
    note: row.note ?? '',
    isSaved,
  }
}

function toNumber(value) {
  const n = Number(value)
  return Number.isFinite(n) ? n : 0
}

function AttendancePage({ embedded = false }) {
  const now = useMemo(() => currentJalaliYearMonth(), [])
  const [year, setYear] = useState(now.year)
  const [month, setMonth] = useState(now.month)
  const [rows, setRows] = useState([])
  const [defaultCoeff, setDefaultCoeff] = useState(DEFAULT_OVERTIME_COEFFICIENT)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [modalOpen, setModalOpen] = useState(false)
  const tableRef = useRef(null)

  const monthOptions = useMemo(
    () =>
      afghanSolarLocale.months.map((m, index) => ({
        value: index + 1,
        label: m[0],
      })),
    [],
  )

  const monthLabel = useMemo(
    () => monthOptions.find((m) => m.value === month)?.label ?? '',
    [month, monthOptions],
  )

  const savedCount = useMemo(
    () => rows.filter((r) => r.isSaved).length,
    [rows],
  )

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const data = await fetchAttendanceMonth(year, month)
      const defaults = countJalaliMonthDaysUntilToday(year, month)
      const nextRows = (data.rows ?? []).map((row) => mapApiRow(row, defaults))
      setDefaultCoeff(
        data.defaultOvertimeCoefficient > 0
          ? data.defaultOvertimeCoefficient
          : DEFAULT_OVERTIME_COEFFICIENT,
      )
      setRows(nextRows)
      return nextRows
    } catch (err) {
      showAppToast(err.message || 'بارگذاری حضور و غیاب با خطا مواجه شد.')
      setRows([])
      return []
    } finally {
      setLoading(false)
    }
  }, [year, month])

  useEffect(() => {
    let cancelled = false

    void fetchAttendanceMonth(year, month)
      .then((data) => {
        if (cancelled) return
        const defaults = countJalaliMonthDaysUntilToday(year, month)
        const nextRows = (data.rows ?? []).map((row) => mapApiRow(row, defaults))
        setDefaultCoeff(
          data.defaultOvertimeCoefficient > 0
            ? data.defaultOvertimeCoefficient
            : DEFAULT_OVERTIME_COEFFICIENT,
        )
        setRows(nextRows)
      })
      .catch((err) => {
        if (cancelled) return
        showAppToast(err.message || 'بارگذاری حضور و غیاب با خطا مواجه شد.')
        setRows([])
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [year, month])

  const updateRow = (employeeId, patch) => {
    setRows((prev) =>
      prev.map((row) =>
        row.employeeId === employeeId ? { ...row, ...patch } : row,
      ),
    )
  }

  const openModal = async () => {
    let currentRows = rows
    if (!currentRows.length) {
      currentRows = await load()
    }
    if (!currentRows.length) {
      showAppToast('کارمند فعالی برای ثبت حضور یافت نشد.', 'warning')
      return
    }
    setModalOpen(true)
  }

  const closeModal = () => {
    if (saving) return
    setModalOpen(false)
  }

  const onSave = async (event) => {
    event?.preventDefault?.()
    if (!rows.length) {
      showAppToast('لیست کارمندان خالی است.', 'warning')
      return
    }

    setSaving(true)
    try {
      const result = await upsertAttendanceMonth({
        year: Number(year),
        month: Number(month),
        items: rows.map((row) => ({
          employeeId: row.employeeId,
          presentDays: toNumber(row.presentDays),
          absentDays: toNumber(row.absentDays),
          leavePaidDays: toNumber(row.leavePaidDays),
          leaveUnpaidDays: toNumber(row.leaveUnpaidDays),
          holidayPaidDays: toNumber(row.holidayPaidDays),
          holidayUnpaidDays: toNumber(row.holidayUnpaidDays),
          lateHours: toNumber(row.lateHours),
          earlyLeaveHours: toNumber(row.earlyLeaveHours),
          overtimeHours: toNumber(row.overtimeHours),
          overtimeCoefficient:
            toNumber(row.overtimeCoefficient) || defaultCoeff,
          note: row.note || null,
        })),
      })
      showAppToast(result.message || 'خلاصه حضور ماه با موفقیت ثبت شد.', 'success')
      setModalOpen(false)
      await load()
    } catch (err) {
      showAppToast(err.message || 'ثبت خلاصه حضور با خطا مواجه شد.')
    } finally {
      setSaving(false)
    }
  }

  const focusNextInput = (current) => {
    const root = tableRef.current
    if (!root || !current) return
    const inputs = Array.from(
      root.querySelectorAll('input:not([disabled]), textarea:not([disabled])'),
    )
    const idx = inputs.indexOf(current)
    if (idx < 0) return
    if (idx < inputs.length - 1) {
      inputs[idx + 1].focus()
      inputs[idx + 1].select?.()
    } else {
      const saveBtn = root
        .closest('.modal-content')
        ?.querySelector('button[type="submit"]')
      saveBtn?.focus()
    }
  }

  const onTableKeyDown = (event) => {
    if (event.key !== 'Enter') return
    if (event.target?.tagName === 'TEXTAREA') return
    event.preventDefault()
    focusNextInput(event.target)
  }

  useEffect(() => {
    if (!modalOpen) return undefined

    const onKeyDown = (event) => {
      if (event.key === 'Escape') {
        event.preventDefault()
        if (!saving) setModalOpen(false)
        return
      }
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 's') {
        event.preventDefault()
        if (!saving && rows.length) {
          void onSave()
        }
      }
    }

    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [modalOpen, saving, rows])

  const content = (
    <div className="card-body p-4">
      <div className="d-flex flex-wrap justify-content-between align-items-start gap-3 mb-3">
        <div>
          <h2 className="card-title mb-1">حضور و غیاب</h2>
          <p className="text-muted mb-0 small">
            برای هر ماه شمسی یک‌بار خلاصه وضعیت کارمندان را وارد کنید.
          </p>
        </div>
        <div className="d-flex flex-wrap align-items-end gap-2">
          <div>
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
          <div>
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
          <button
            type="button"
            className="btn btn-outline-secondary"
            onClick={load}
            disabled={loading}
          >
            بارگذاری
          </button>
          <button
            type="button"
            className="btn btn-primary"
            onClick={openModal}
            disabled={loading}
          >
            ثبت / ویرایش خلاصه ماه
          </button>
        </div>
      </div>

      <div className="border rounded p-3 bg-light-subtle">
        <div className="d-flex flex-wrap justify-content-between gap-2">
          <div>
            <div className="fw-semibold">
              {monthLabel} {year}
            </div>
            <div className="text-muted small mt-1">
              {loading
                ? 'در حال بارگذاری...'
                : `${rows.length} کارمند فعال — ${savedCount} نفر ثبت‌شده`}
            </div>
          </div>
          <button
            type="button"
            className="btn btn-outline-primary align-self-center"
            onClick={openModal}
            disabled={loading || !rows.length}
          >
            باز کردن فرم ماه
          </button>
        </div>
      </div>
    </div>
  )

  return (
    <div className="users-page attendance-page">
      {embedded ? content : <div className="content-card card border-0 mb-3">{content}</div>}

      {modalOpen && (
        <div
          className="modal show d-block attendance-modal"
          tabIndex={-1}
          style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}
          onMouseDown={(e) => {
            if (e.target === e.currentTarget) closeModal()
          }}
        >
          <div className="modal-dialog modal-fullscreen-lg-down modal-xl modal-dialog-scrollable">
            <div className="modal-content">
              <form onSubmit={onSave}>
                <div className="modal-header">
                  <h5 className="modal-title">
                    خلاصه حضور — {monthLabel} {year}
                  </h5>
                  <button
                    type="button"
                    className="btn-close"
                    aria-label="بستن"
                    onClick={closeModal}
                    disabled={saving}
                  />
                </div>
                <div className="modal-body p-0">
                  <div className="table-responsive attendance-month-table-wrap">
                    <table
                      ref={tableRef}
                      className="table table-sm table-bordered align-middle mb-0 attendance-month-table"
                      onKeyDown={onTableKeyDown}
                    >
                      <thead>
                        <tr>
                          <th className="attendance-col-name">کارمند</th>
                          <th>حاضر</th>
                          <th>غیرحاضر</th>
                          <th>رخصت با حقوق</th>
                          <th>رخصت بدون حقوق</th>
                          <th>تعطیل با حقوق</th>
                          <th>تعطیل بدون حقوق</th>
                          <th>تأخیر (ساعت)</th>
                          <th>تعجیل خروج (ساعت)</th>
                          <th>اضافه‌کار (ساعت)</th>
                          <th>ضریب اضافه‌کار</th>
                          <th className="attendance-col-note">توضیحات</th>
                        </tr>
                      </thead>
                      <tbody>
                        {rows.length === 0 && (
                          <tr>
                            <td colSpan={12} className="text-center text-muted py-4">
                              کارمند فعالی یافت نشد.
                            </td>
                          </tr>
                        )}
                        {rows.map((row) => (
                          <tr key={row.employeeId}>
                            <td className="attendance-col-name">
                              <div className="fw-semibold">{row.fullName}</div>
                              <div className="text-muted small">
                                {row.departmentName || '—'}
                              </div>
                            </td>
                            {[
                              ['presentDays', 'حاضر'],
                              ['absentDays', 'غیرحاضر'],
                              ['leavePaidDays', 'رخصت با حقوق'],
                              ['leaveUnpaidDays', 'رخصت بدون حقوق'],
                              ['holidayPaidDays', 'تعطیل با حقوق'],
                              ['holidayUnpaidDays', 'تعطیل بدون حقوق'],
                            ].map(([key, label]) => (
                              <td key={key}>
                                <input
                                  type="number"
                                  min={0}
                                  step={1}
                                  className="form-control form-control-sm"
                                  value={row[key]}
                                  aria-label={`${label} ${row.fullName}`}
                                  onChange={(e) =>
                                    updateRow(row.employeeId, {
                                      [key]: e.target.value,
                                    })
                                  }
                                />
                              </td>
                            ))}
                            {[
                              ['lateHours', 'تأخیر'],
                              ['earlyLeaveHours', 'تعجیل خروج'],
                              ['overtimeHours', 'اضافه‌کار'],
                              ['overtimeCoefficient', 'ضریب اضافه‌کار'],
                            ].map(([key, label]) => (
                              <td key={key}>
                                <input
                                  type="number"
                                  min={0}
                                  step={0.01}
                                  className="form-control form-control-sm"
                                  value={row[key]}
                                  aria-label={`${label} ${row.fullName}`}
                                  onChange={(e) =>
                                    updateRow(row.employeeId, {
                                      [key]: e.target.value,
                                    })
                                  }
                                />
                              </td>
                            ))}
                            <td className="attendance-col-note">
                              <input
                                type="text"
                                className="form-control form-control-sm"
                                value={row.note}
                                maxLength={2000}
                                aria-label={`توضیحات ${row.fullName}`}
                                onChange={(e) =>
                                  updateRow(row.employeeId, {
                                    note: e.target.value,
                                  })
                                }
                              />
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
                <div className="modal-footer">
                  <span className="me-auto text-muted small">
                    Ctrl+S ذخیره — Esc بستن — Enter فیلد بعدی
                  </span>
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    onClick={closeModal}
                    disabled={saving}
                  >
                    بستن
                  </button>
                  <button
                    type="submit"
                    className="btn btn-primary"
                    disabled={saving || !rows.length}
                  >
                    {saving ? 'در حال ذخیره...' : 'ذخیره خلاصه ماه'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

export default AttendancePage
