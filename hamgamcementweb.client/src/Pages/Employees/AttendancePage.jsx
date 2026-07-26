import { useCallback, useEffect, useMemo, useState } from 'react'
import JalaliDateField from '../../components/common/JalaliDateField'
import {
  formatJalaliDate,
  todayGregorianIso,
} from '../../lib/afghanSolarCalendar'
import { fetchAttendanceRange, upsertAttendanceDay } from '../../services/hrApi'

function AttendancePage({ embedded = false }) {
  const [date, setDate] = useState(todayGregorianIso)
  const [employees, setEmployees] = useState([])
  const [rows, setRows] = useState([])
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')

  const load = useCallback(async () => {
    if (!date) return
    setLoading(true)
    setError('')
    setMessage('')
    try {
      // برای یک روز کافی است from = to = همان تاریخ
      const data = await fetchAttendanceRange(date, date)
      const byEmployee = new Map(
        (data.attendances ?? []).map((a) => [a.employeeId, a]),
      )

      const nextRows = (data.employees ?? []).map((emp) => {
        const saved = byEmployee.get(emp.employeeId)
        return {
          employeeId: emp.employeeId,
          fullName: emp.fullName,
          departmentName: emp.departmentName,
          isPresent: saved?.isPresent ?? false,
          lateMinutes: saved?.lateMinutes ?? 0,
          overtimeMinutes: saved?.overtimeMinutes ?? 0,
          note: saved?.note ?? '',
        }
      })

      setEmployees(data.employees ?? [])
      setRows(nextRows)
    } catch (err) {
      setError(err.message)
      setEmployees([])
      setRows([])
    } finally {
      setLoading(false)
    }
  }, [date])

  useEffect(() => {
    load()
  }, [load])

  const presentCount = useMemo(
    () => rows.filter((r) => r.isPresent).length,
    [rows],
  )

  const updateRow = (employeeId, patch) => {
    setRows((prev) =>
      prev.map((row) =>
        row.employeeId === employeeId ? { ...row, ...patch } : row,
      ),
    )
  }

  const togglePresent = (employeeId, checked) => {
    setRows((prev) =>
      prev.map((row) => {
        if (row.employeeId !== employeeId) return row
        if (checked) return { ...row, isPresent: true }
        return {
          ...row,
          isPresent: false,
          lateMinutes: 0,
          overtimeMinutes: 0,
        }
      }),
    )
  }

  const markAllPresent = () => {
    setRows((prev) =>
      prev.map((row) => ({
        ...row,
        isPresent: true,
      })),
    )
  }

  const clearAll = () => {
    setRows((prev) =>
      prev.map((row) => ({
        ...row,
        isPresent: false,
        lateMinutes: 0,
        overtimeMinutes: 0,
      })),
    )
  }

  const onSave = async (event) => {
    event.preventDefault()
    setSaving(true)
    setError('')
    setMessage('')
    try {
      const result = await upsertAttendanceDay({
        date,
        items: rows.map((row) => ({
          employeeId: row.employeeId,
          isPresent: row.isPresent,
          lateMinutes: Number(row.lateMinutes) || 0,
          overtimeMinutes: Number(row.overtimeMinutes) || 0,
          note: row.note || null,
        })),
      })
      setMessage(result.message)
      await load()
    } catch (err) {
      setError(err.message)
    } finally {
      setSaving(false)
    }
  }

  const content = (
    <div className="card-body p-4">
      <div className="d-flex flex-wrap justify-content-between align-items-start gap-3 mb-3">
        <div>
          <h2 className="card-title mb-1">حضور و غیاب</h2>
          <p className="text-muted mb-0 small">
            برای هر روز تیک بزنید؛ دیرکرد و اضافه‌کاری را به دقیقه وارد کنید.
          </p>
        </div>
        <div className="d-flex flex-wrap align-items-end gap-2">
          <div>
            <label className="form-label mb-1">تاریخ</label>
            <JalaliDateField value={date} onChange={setDate} required />
          </div>
          <button
            type="button"
            className="btn btn-outline-secondary"
            onClick={load}
            disabled={loading}
          >
            بارگذاری
          </button>
        </div>
      </div>

      {message && <div className="alert alert-success py-2">{message}</div>}
      {error && <div className="alert alert-danger py-2">{error}</div>}

      <div className="d-flex flex-wrap gap-2 mb-3">
        <button
          type="button"
          className="btn btn-sm btn-outline-primary"
          onClick={markAllPresent}
          disabled={!rows.length}
        >
          همه حاضر
        </button>
        <button
          type="button"
          className="btn btn-sm btn-outline-secondary"
          onClick={clearAll}
          disabled={!rows.length}
        >
          پاک کردن تیک‌ها
        </button>
        <span className="align-self-center text-muted small">
          {formatJalaliDate(date)} — حاضر: {presentCount} از {employees.length}
        </span>
      </div>

      <form onSubmit={onSave}>
        <div className="table-responsive">
          <table className="table table-sm table-hover align-middle mb-3">
            <thead>
              <tr>
                <th style={{ width: 56 }} className="text-center">
                  حضور
                </th>
                <th>کارمند</th>
                <th>بخش</th>
                <th style={{ width: 120 }}>دیرکرد (دقیقه)</th>
                <th style={{ width: 120 }}>اضافه‌کار (دقیقه)</th>
                <th>یادداشت</th>
              </tr>
            </thead>
            <tbody>
              {loading && (
                <tr>
                  <td colSpan={6} className="text-center text-muted py-4">
                    در حال بارگذاری...
                  </td>
                </tr>
              )}
              {!loading && rows.length === 0 && (
                <tr>
                  <td colSpan={6} className="text-center text-muted py-4">
                    کارمند فعالی یافت نشد.
                  </td>
                </tr>
              )}
              {!loading &&
                rows.map((row) => (
                  <tr key={row.employeeId}>
                    <td className="text-center">
                      <input
                        type="checkbox"
                        className="form-check-input"
                        checked={row.isPresent}
                        onChange={(e) =>
                          togglePresent(row.employeeId, e.target.checked)
                        }
                        aria-label={`حضور ${row.fullName}`}
                      />
                    </td>
                    <td>{row.fullName}</td>
                    <td>{row.departmentName || '—'}</td>
                    <td>
                      <input
                        type="number"
                        min={0}
                        className="form-control form-control-sm"
                        value={row.lateMinutes}
                        disabled={!row.isPresent}
                        onChange={(e) =>
                          updateRow(row.employeeId, {
                            lateMinutes: e.target.value,
                          })
                        }
                      />
                    </td>
                    <td>
                      <input
                        type="number"
                        min={0}
                        className="form-control form-control-sm"
                        value={row.overtimeMinutes}
                        disabled={!row.isPresent}
                        onChange={(e) =>
                          updateRow(row.employeeId, {
                            overtimeMinutes: e.target.value,
                          })
                        }
                      />
                    </td>
                    <td>
                      <input
                        type="text"
                        className="form-control form-control-sm"
                        value={row.note}
                        onChange={(e) =>
                          updateRow(row.employeeId, { note: e.target.value })
                        }
                      />
                    </td>
                  </tr>
                ))}
            </tbody>
          </table>
        </div>

        <button
          type="submit"
          className="btn btn-primary"
          disabled={saving || loading || !rows.length}
        >
          {saving ? 'در حال ذخیره...' : 'ذخیره حضور روز'}
        </button>
      </form>
    </div>
  )

  return (
    <div className="users-page">
      {embedded ? content : <div className="content-card card border-0 mb-3">{content}</div>}
    </div>
  )
}

export default AttendancePage
