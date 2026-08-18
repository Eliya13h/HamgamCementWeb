import { useEffect, useState } from 'react'
import JalaliDateField from '../../components/common/JalaliDateField'
import { persianValidity, validateFormPersian } from '../../lib/persianFormValidity'
import { showAppToast } from '../../lib/appToast'
import {
  costCentersApi,
  fetchAccountTree,
  recurringJournalsApi,
} from '../../services/ledgerApi'

const emptyLine = () => ({
  accountId: '',
  description: '',
  debitInBaseCurrency: '',
  creditInBaseCurrency: '',
  costCenterId: '',
})

export default function RecurringJournalsPage() {
  const [rows, setRows] = useState([])
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10))
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [showCreate, setShowCreate] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [accounts, setAccounts] = useState([])
  const [costCenters, setCostCenters] = useState([])
  const [form, setForm] = useState({
    code: '',
    name: '',
    description: '',
    isActive: true,
    lines: [emptyLine(), emptyLine()],
  })

  const load = () =>
    recurringJournalsApi
      .list()
      .then(setRows)
      .catch((e) => setError(e.message))

  useEffect(() => {
    load()
    Promise.all([fetchAccountTree(), costCentersApi.options()])
      .then(([tree, centers]) => {
        setAccounts((tree ?? []).filter((a) => a.isPostable))
        setCostCenters(centers ?? [])
      })
      .catch(() => {})
  }, [])

  const generate = async (id) => {
    setError('')
    try {
      const result = await recurringJournalsApi.generate(id, { entryDate: date })
      setMessage(result.message ?? 'سند صادر شد.')
    } catch (e) {
      setError(e.message)
    }
  }

  const remove = async (id) => {
    if (!window.confirm('قالب حذف شود؟')) return
    try {
      await recurringJournalsApi.remove(id)
      load()
    } catch (e) {
      setError(e.message)
    }
  }

  const updateLine = (index, patch) => {
    setForm((prev) => ({
      ...prev,
      lines: prev.lines.map((line, i) => (i === index ? { ...line, ...patch } : line)),
    }))
  }

  const handleCreate = async (event) => {
    event.preventDefault()
    const formEl = event.currentTarget
    const validity = validateFormPersian(formEl)
    if (validity) {
      showAppToast(validity)
      formEl.reportValidity()
      return
    }

    const lines = form.lines
      .filter((l) => l.accountId)
      .map((l) => ({
        accountId: Number(l.accountId),
        description: l.description.trim() || null,
        debitInBaseCurrency: Number(l.debitInBaseCurrency) || 0,
        creditInBaseCurrency: Number(l.creditInBaseCurrency) || 0,
        costCenterId: l.costCenterId ? Number(l.costCenterId) : null,
      }))

    if (lines.length < 2) {
      setError('قالب باید حداقل دو ردیف داشته باشد.')
      return
    }

    setSubmitting(true)
    setError('')
    try {
      await recurringJournalsApi.create({
        code: form.code.trim(),
        name: form.name.trim(),
        description: form.description.trim() || null,
        isActive: form.isActive,
        lines,
      })
      setShowCreate(false)
      setForm({
        code: '',
        name: '',
        description: '',
        isActive: true,
        lines: [emptyLine(), emptyLine()],
      })
      setMessage('قالب ثبت شد.')
      load()
    } catch (e) {
      setError(e.message)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="users-page">
      <div className="content-card card border-0">
        <div className="card-header bg-transparent border-0 pt-4 px-4 d-flex justify-content-between align-items-end flex-wrap gap-3">
          <div>
            <h2 className="card-title mb-1">اسناد تکرارشونده</h2>
            <p className="text-muted mb-0 small">
              قالب‌های ثبت‌شده را در تاریخ انتخابی به سند دفتر تبدیل کنید.
            </p>
          </div>
          <div className="d-flex gap-2 align-items-end">
            <div>
              <label className="form-label small">تاریخ صدور</label>
              <JalaliDateField value={date} onChange={setDate} />
            </div>
            <button
              type="button"
              className="btn btn-accent"
              onClick={() => {
                setError('')
                setShowCreate(true)
              }}
            >
              قالب جدید
            </button>
          </div>
        </div>
        <div className="card-body p-4">
          {error && <div className="alert alert-danger py-2">{error}</div>}
          {message && <div className="alert alert-success py-2">{message}</div>}
          <div className="table-responsive">
            <table className="table align-middle">
              <thead>
                <tr>
                  <th>کد</th>
                  <th>نام</th>
                  <th>توضیحات</th>
                  <th>ردیف‌ها</th>
                  <th>وضعیت</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => (
                  <tr key={row.recurringJournalTemplateId}>
                    <td>{row.code}</td>
                    <td>{row.name}</td>
                    <td>{row.description || '—'}</td>
                    <td>{row.lineCount}</td>
                    <td>{row.isActive ? 'فعال' : 'غیرفعال'}</td>
                    <td className="d-flex gap-2">
                      <button
                        type="button"
                        className="btn btn-sm btn-accent"
                        disabled={!row.isActive}
                        onClick={() => generate(row.recurringJournalTemplateId)}
                      >
                        صدور سند
                      </button>
                      <button
                        type="button"
                        className="btn btn-sm btn-outline-danger"
                        onClick={() => remove(row.recurringJournalTemplateId)}
                      >
                        حذف
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {showCreate && (
        <>
          <div
            className="modal-backdrop show users-modal-backdrop"
            onClick={() => setShowCreate(false)}
          />
          <div className="modal show d-block users-modal" tabIndex="-1" role="dialog" aria-modal="true">
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable modal-xl">
              <div className="modal-content">
                <form onSubmit={handleCreate}>
                  <div className="modal-header border-0 pb-0">
                    <h5 className="modal-title">قالب سند تکرارشونده</h5>
                    <button
                      type="button"
                      className="btn-close"
                      aria-label="بستن"
                      onClick={() => setShowCreate(false)}
                    />
                  </div>
                  <div className="modal-body pt-3">
                    <div className="row g-3 mb-3">
                      <div className="col-md-3">
                        <label className="form-label">کد</label>
                        <input
                          className="form-control"
                          value={form.code}
                          onChange={(e) => setForm((p) => ({ ...p, code: e.target.value }))}
                          required
                          {...persianValidity('کد الزامی است.')}
                        />
                      </div>
                      <div className="col-md-5">
                        <label className="form-label">نام</label>
                        <input
                          className="form-control"
                          value={form.name}
                          onChange={(e) => setForm((p) => ({ ...p, name: e.target.value }))}
                          required
                          {...persianValidity('نام الزامی است.')}
                        />
                      </div>
                      <div className="col-md-4">
                        <label className="form-label">توضیحات</label>
                        <input
                          className="form-control"
                          value={form.description}
                          onChange={(e) =>
                            setForm((p) => ({ ...p, description: e.target.value }))
                          }
                        />
                      </div>
                    </div>

                    <div className="table-responsive border rounded-3">
                      <table className="table table-sm align-middle mb-0">
                        <thead className="table-light">
                          <tr>
                            <th style={{ minWidth: 200 }}>حساب</th>
                            <th style={{ minWidth: 150 }}>مرکز هزینه</th>
                            <th style={{ width: 120 }}>دیبت (Db)</th>
                            <th style={{ width: 120 }}>کریدیت (Cr)</th>
                            <th>شرح</th>
                            <th style={{ width: 60 }} />
                          </tr>
                        </thead>
                        <tbody>
                          {form.lines.map((line, index) => (
                            <tr key={index}>
                              <td>
                                <select
                                  className="form-select form-select-sm"
                                  value={line.accountId}
                                  onChange={(e) =>
                                    updateLine(index, { accountId: e.target.value })
                                  }
                                  required
                                  {...persianValidity('حساب را انتخاب کنید.')}
                                >
                                  <option value="">انتخاب...</option>
                                  {accounts.map((acc) => (
                                    <option key={acc.accountId} value={acc.accountId}>
                                      {acc.code} — {acc.name}
                                    </option>
                                  ))}
                                </select>
                              </td>
                              <td>
                                <select
                                  className="form-select form-select-sm"
                                  value={line.costCenterId}
                                  onChange={(e) =>
                                    updateLine(index, { costCenterId: e.target.value })
                                  }
                                >
                                  <option value="">—</option>
                                  {costCenters.map((c) => (
                                    <option key={c.value} value={c.value}>
                                      {c.label}
                                    </option>
                                  ))}
                                </select>
                              </td>
                              <td>
                                <input
                                  type="number"
                                  min="0"
                                  step="any"
                                  className="form-control form-control-sm text-end"
                                  value={line.debitInBaseCurrency}
                                  onChange={(e) =>
                                    updateLine(index, {
                                      debitInBaseCurrency: e.target.value,
                                      creditInBaseCurrency: e.target.value
                                        ? ''
                                        : line.creditInBaseCurrency,
                                    })
                                  }
                                />
                              </td>
                              <td>
                                <input
                                  type="number"
                                  min="0"
                                  step="any"
                                  className="form-control form-control-sm text-end"
                                  value={line.creditInBaseCurrency}
                                  onChange={(e) =>
                                    updateLine(index, {
                                      creditInBaseCurrency: e.target.value,
                                      debitInBaseCurrency: e.target.value
                                        ? ''
                                        : line.debitInBaseCurrency,
                                    })
                                  }
                                />
                              </td>
                              <td>
                                <input
                                  className="form-control form-control-sm"
                                  value={line.description}
                                  onChange={(e) =>
                                    updateLine(index, { description: e.target.value })
                                  }
                                />
                              </td>
                              <td>
                                <button
                                  type="button"
                                  className="btn btn-sm btn-outline-danger"
                                  disabled={form.lines.length <= 2}
                                  onClick={() =>
                                    setForm((p) => ({
                                      ...p,
                                      lines: p.lines.filter((_, i) => i !== index),
                                    }))
                                  }
                                >
                                  ×
                                </button>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                    <button
                      type="button"
                      className="btn btn-sm btn-outline-secondary mt-2"
                      onClick={() =>
                        setForm((p) => ({ ...p, lines: [...p.lines, emptyLine()] }))
                      }
                    >
                      افزودن ردیف
                    </button>
                  </div>
                  <div className="modal-footer border-0">
                    <button
                      type="button"
                      className="btn btn-outline-secondary"
                      onClick={() => setShowCreate(false)}
                    >
                      بستن
                    </button>
                    <button type="submit" className="btn btn-accent" disabled={submitting}>
                      {submitting ? 'در حال ذخیره...' : 'ذخیره قالب'}
                    </button>
                  </div>
                </form>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  )
}
