import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import AmountDisplay from '../../components/common/AmountDisplay'
import JalaliDateField from '../../components/common/JalaliDateField'
import {
  useModalKeyboardShortcuts,
  useModalAutoFocus,
} from '../../hooks/useModalKeyboardShortcuts'
import { showAppToast } from '../../lib/appToast'
import {
  currentJalaliYearMonth,
  formatJalaliDate,
  getJalaliYearRange,
  todayGregorianIso,
  toLatinIsoDate,
} from '../../lib/afghanSolarCalendar'
import { persianValidity, validateFormPersian } from '../../lib/persianFormValidity'
import {
  accountsApi,
  fetchAccountLedger,
  fetchAccountTree,
} from '../../services/ledgerApi'

const LEVEL_LABEL = { 1: 'گروه', 2: 'کل', 3: 'معین', 4: 'تفصیلی' }
const TAFSILI_LEVEL = 4

const emptyForm = {
  parentAccountId: '',
  name: '',
  code: '',
  description: '',
  isPostable: false,
}

function AccountsPage() {
  const formRef = useRef(null)
  const [rows, setRows] = useState([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  const [formMode, setFormMode] = useState(null) // 'create' | 'edit'
  const [editing, setEditing] = useState(null)
  const [form, setForm] = useState(emptyForm)
  const [formError, setFormError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const [ledgerAccount, setLedgerAccount] = useState(null)
  const [ledgerFrom, setLedgerFrom] = useState('')
  const [ledgerTo, setLedgerTo] = useState('')
  const [ledgerData, setLedgerData] = useState(null)
  const [ledgerError, setLedgerError] = useState('')
  const [ledgerLoading, setLedgerLoading] = useState(false)

  const yearStart = useMemo(() => {
    const { year } = currentJalaliYearMonth()
    return getJalaliYearRange(year).from
  }, [])

  const loadTree = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const data = await fetchAccountTree()
      setRows(data ?? [])
    } catch (e) {
      setError(e.message)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadTree()
  }, [loadTree])

  const tree = useMemo(() => {
    const byParent = new Map()
    for (const row of rows) {
      const key = row.parentAccountId ?? 0
      if (!byParent.has(key)) byParent.set(key, [])
      byParent.get(key).push(row)
    }
    const walk = (parentId, depth) => {
      const children = byParent.get(parentId) ?? []
      return children.flatMap((node) => [
        { ...node, depth },
        ...walk(node.accountId, depth + 1),
      ])
    }
    return walk(0, 0)
  }, [rows])

  const parentOptions = useMemo(
    () => rows.filter((r) => Number(r.level) < TAFSILI_LEVEL),
    [rows],
  )

  const closeForm = () => {
    setFormMode(null)
    setEditing(null)
    setForm(emptyForm)
    setFormError('')
    setSubmitting(false)
  }

  const openCreate = (parent) => {
    const defaultPostable = Number(parent.level) >= 2
    setFormMode('create')
    setEditing(null)
    setForm({
      parentAccountId: String(parent.accountId),
      name: '',
      code: '',
      description: '',
      isPostable: defaultPostable,
    })
    setFormError('')
  }

  const openEdit = async (row) => {
    setFormMode('edit')
    setEditing(row)
    setForm({
      parentAccountId: row.parentAccountId ? String(row.parentAccountId) : '',
      name: row.name ?? '',
      code: row.code ?? '',
      description: row.description ?? '',
      isPostable: Boolean(row.isPostable),
    })
    setFormError('')
    try {
      const detail = await accountsApi.get(row.accountId)
      setEditing(detail)
      setForm({
        parentAccountId: detail.parentAccountId
          ? String(detail.parentAccountId)
          : '',
        name: detail.name ?? '',
        code: detail.code ?? '',
        description: detail.description ?? '',
        isPostable: Boolean(detail.isPostable),
      })
    } catch (e) {
      setFormError(e.message)
    }
  }

  const handleDelete = async (row) => {
    if (row.isSystem) return
    if (!window.confirm(`حساب «${row.name}» حذف شود؟`)) return
    try {
      await accountsApi.remove(row.accountId)
      await loadTree()
    } catch (e) {
      setError(e.message)
    }
  }

  const openLedger = (row) => {
    setLedgerAccount(row)
    setLedgerFrom(yearStart)
    setLedgerTo(todayGregorianIso())
    setLedgerData(null)
    setLedgerError('')
  }

  const closeLedger = () => {
    setLedgerAccount(null)
    setLedgerData(null)
    setLedgerError('')
    setLedgerLoading(false)
  }

  const triggerFormSave = useCallback(() => {
    if (!submitting) formRef.current?.requestSubmit()
  }, [submitting])

  useModalKeyboardShortcuts({
    open: Boolean(formMode),
    onClose: closeForm,
    onSave: triggerFormSave,
    formRef,
  })
  useModalKeyboardShortcuts({
    open: Boolean(ledgerAccount),
    onClose: closeLedger,
  })
  useModalAutoFocus({ open: Boolean(formMode), formRef })

  const handleSubmit = async (event) => {
    event.preventDefault()
    const formEl = event.currentTarget
    const message = validateFormPersian(formEl)
    if (message) {
      showAppToast(message)
      formEl.reportValidity()
      return
    }
    setFormError('')
    if (!form.name.trim()) {
      setFormError('نام حساب الزامی است.')
      return
    }
    if (formMode === 'create' && !form.parentAccountId) {
      setFormError('حساب والد الزامی است.')
      return
    }

    setSubmitting(true)
    try {
      if (formMode === 'create') {
        await accountsApi.create({
          parentAccountId: Number(form.parentAccountId),
          name: form.name.trim(),
          code: form.code.trim() || null,
          description: form.description.trim() || null,
          isPostable: form.isPostable,
        })
      } else if (editing) {
        const payload = editing.isSystem
          ? {
              parentAccountId: editing.parentAccountId ?? 0,
              name: form.name.trim(),
              description: form.description.trim() || null,
            }
          : {
              parentAccountId: editing.parentAccountId ?? 0,
              name: form.name.trim(),
              code: form.code.trim() || null,
              description: form.description.trim() || null,
              isPostable: form.isPostable,
            }
        await accountsApi.update(editing.accountId, payload)
      }
      closeForm()
      await loadTree()
    } catch (e) {
      setFormError(e.message)
    } finally {
      setSubmitting(false)
    }
  }

  const loadLedger = useCallback(async () => {
    if (!ledgerAccount) return
    setLedgerLoading(true)
    setLedgerError('')
    try {
      const data = await fetchAccountLedger(ledgerAccount.accountId, {
        dateFrom: toLatinIsoDate(ledgerFrom) || undefined,
        dateTo: toLatinIsoDate(ledgerTo) || undefined,
      })
      setLedgerData(data)
    } catch (e) {
      setLedgerData(null)
      setLedgerError(e.message)
    } finally {
      setLedgerLoading(false)
    }
  }, [ledgerAccount, ledgerFrom, ledgerTo])

  useEffect(() => {
    if (ledgerAccount) {
      loadLedger()
    }
  }, [ledgerAccount, loadLedger])

  const isSystemEdit = formMode === 'edit' && editing?.isSystem

  return (
    <div className="content-card card border-0">
      <div className="card-body p-4">
        <div className="d-flex align-items-center justify-content-between gap-3 flex-wrap mb-3">
          <h2 className="card-title mb-0">کدینگ حساب‌ها</h2>
          <button
            type="button"
            className="btn btn-sm btn-outline-secondary"
            onClick={loadTree}
            disabled={loading}
          >
            بروزرسانی
          </button>
        </div>

        {error ? <div className="alert alert-danger">{error}</div> : null}

        {loading ? (
          <div className="text-muted">در حال بارگذاری...</div>
        ) : (
          <div className="table-responsive">
            <table className="table table-sm table-hover align-middle mb-0">
              <thead>
                <tr>
                  <th>کد</th>
                  <th>نام</th>
                  <th>سطح</th>
                  <th>قابل ثبت</th>
                  <th className="text-center" style={{ minWidth: 260 }}>
                    عملیات
                  </th>
                </tr>
              </thead>
              <tbody>
                {tree.map((row) => (
                  <tr key={row.accountId}>
                    <td
                      style={{
                        paddingInlineStart: `${row.depth * 1.25}rem`,
                        fontFamily: 'monospace',
                      }}
                    >
                      {row.code}
                      {row.isSystem ? (
                        <span className="badge text-bg-light border ms-2">سیستمی</span>
                      ) : null}
                    </td>
                    <td>{row.name}</td>
                    <td>{LEVEL_LABEL[row.level] ?? row.level}</td>
                    <td>{row.isPostable ? 'بله' : '—'}</td>
                    <td className="text-center">
                      <div className="d-inline-flex flex-wrap gap-1 justify-content-center">
                        {Number(row.level) < TAFSILI_LEVEL && (
                          <button
                            type="button"
                            className="btn btn-sm btn-outline-success"
                            onClick={() => openCreate(row)}
                          >
                            افزودن زیرحساب
                          </button>
                        )}
                        <button
                          type="button"
                          className="btn btn-sm btn-outline-primary"
                          onClick={() => openEdit(row)}
                        >
                          ویرایش
                        </button>
                        {!row.isSystem && (
                          <button
                            type="button"
                            className="btn btn-sm btn-outline-danger"
                            onClick={() => handleDelete(row)}
                          >
                            حذف
                          </button>
                        )}
                        <button
                          type="button"
                          className="btn btn-sm btn-outline-secondary"
                          onClick={() => openLedger(row)}
                        >
                          گردش حساب
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
                {tree.length === 0 && (
                  <tr>
                    <td colSpan={5} className="text-center text-muted py-4">
                      حسابی یافت نشد.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {formMode && (
        <>
          <div className="modal-backdrop show users-modal-backdrop" onClick={closeForm} />
          <div
            className="modal show d-block users-modal"
            tabIndex="-1"
            role="dialog"
            aria-modal="true"
          >
            <div className="modal-dialog modal-dialog-centered modal-lg">
              <form ref={formRef} className="modal-content" onSubmit={handleSubmit} noValidate>
                <div className="modal-header border-0 pb-0">
                  <h5 className="modal-title">
                    {formMode === 'create'
                      ? 'افزودن زیرحساب'
                      : isSystemEdit
                        ? 'ویرایش حساب سیستمی'
                        : 'ویرایش حساب'}
                  </h5>
                  <button
                    type="button"
                    className="btn-close"
                    aria-label="بستن"
                    onClick={closeForm}
                  />
                </div>
                <div className="modal-body">
                  {formError ? (
                    <div className="alert alert-danger py-2">{formError}</div>
                  ) : null}
                  {isSystemEdit ? (
                    <div className="alert alert-info py-2">
                      برای حساب سیستمی فقط نام و شرح قابل ویرایش است.
                    </div>
                  ) : null}

                  <div className="row g-3">
                    <div className="col-12">
                      <label className="form-label mb-1">حساب والد</label>
                      <select
                        className="form-select"
                        value={form.parentAccountId}
                        disabled={formMode === 'edit'}
                        required={formMode === 'create'}
                        {...(formMode === 'create'
                          ? persianValidity('لطفاً حساب والد را انتخاب کنید.')
                          : {})}
                        onChange={(e) => {
                          e.target.setCustomValidity('')
                          setForm((prev) => ({
                            ...prev,
                            parentAccountId: e.target.value,
                          }))
                        }}
                      >
                        <option value="">انتخاب کنید...</option>
                        {parentOptions.map((opt) => (
                          <option key={opt.accountId} value={opt.accountId}>
                            {opt.code} — {opt.name}
                          </option>
                        ))}
                      </select>
                    </div>

                    <div className="col-md-6">
                      <label className="form-label mb-1">نام</label>
                      <input
                        type="text"
                        className="form-control"
                        value={form.name}
                        required
                        {...persianValidity('لطفاً نام حساب را وارد کنید.')}
                        onChange={(e) => {
                          e.target.setCustomValidity('')
                          setForm((prev) => ({ ...prev, name: e.target.value }))
                        }}
                      />
                    </div>

                    <div className="col-md-6">
                      <label className="form-label mb-1">کد (اختیاری)</label>
                      <input
                        type="text"
                        className="form-control"
                        value={form.code}
                        disabled={isSystemEdit}
                        onChange={(e) =>
                          setForm((prev) => ({ ...prev, code: e.target.value }))
                        }
                        placeholder="در صورت خالی بودن، خودکار تولید می‌شود"
                      />
                    </div>

                    <div className="col-12">
                      <label className="form-label mb-1">شرح</label>
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

                    <div className="col-12">
                      <div className="form-check">
                        <input
                          id="account-is-postable"
                          type="checkbox"
                          className="form-check-input"
                          checked={form.isPostable}
                          disabled={isSystemEdit}
                          onChange={(e) =>
                            setForm((prev) => ({
                              ...prev,
                              isPostable: e.target.checked,
                            }))
                          }
                        />
                        <label className="form-check-label" htmlFor="account-is-postable">
                          قابل ثبت (پست‌پذیر)
                        </label>
                      </div>
                    </div>
                  </div>
                </div>
                <div className="modal-footer border-0 pt-0">
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    onClick={closeForm}
                    disabled={submitting}
                  >
                    انصراف
                  </button>
                  <button
                    type="submit"
                    className="btn btn-primary"
                    disabled={submitting}
                  >
                    {submitting ? 'در حال ذخیره...' : 'ذخیره'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </>
      )}

      {ledgerAccount && (
        <>
          <div className="modal-backdrop show users-modal-backdrop" onClick={closeLedger} />
          <div
            className="modal show d-block users-modal"
            tabIndex="-1"
            role="dialog"
            aria-modal="true"
          >
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable modal-xl">
              <div className="modal-content">
                <div className="modal-header border-0 pb-0">
                  <div>
                    <h5 className="modal-title mb-1">گردش حساب</h5>
                    <div className="text-muted small">
                      <span className="font-monospace">{ledgerAccount.code}</span>
                      {' — '}
                      {ledgerAccount.name}
                    </div>
                  </div>
                  <button
                    type="button"
                    className="btn-close"
                    aria-label="بستن"
                    onClick={closeLedger}
                  />
                </div>
                <div className="modal-body pt-3">
                  <div className="row g-3 align-items-end mb-3">
                    <div className="col-md-4">
                      <label className="form-label">از تاریخ</label>
                      <JalaliDateField value={ledgerFrom} onChange={setLedgerFrom} />
                    </div>
                    <div className="col-md-4">
                      <label className="form-label">تا تاریخ</label>
                      <JalaliDateField value={ledgerTo} onChange={setLedgerTo} />
                    </div>
                    <div className="col-md-4">
                      <button
                        type="button"
                        className="btn btn-primary w-100"
                        onClick={loadLedger}
                        disabled={ledgerLoading}
                      >
                        {ledgerLoading ? 'در حال بارگذاری...' : 'نمایش گردش'}
                      </button>
                    </div>
                  </div>

                  {ledgerError ? (
                    <div className="alert alert-danger py-2">{ledgerError}</div>
                  ) : null}

                  {ledgerLoading && !ledgerData ? (
                    <div className="text-center text-muted py-4">
                      در حال بارگذاری گردش حساب...
                    </div>
                  ) : null}

                  {ledgerData && (
                    <>
                      <div className="row g-3 mb-3">
                        <div className="col-md-4">
                          <div className="border rounded-3 p-3 bg-light bg-opacity-50 h-100">
                            <div className="text-muted small mb-1">مانده اول دوره</div>
                            <div className="fw-semibold">
                              <AmountDisplay value={ledgerData.openingBalance} />
                            </div>
                          </div>
                        </div>
                        <div className="col-md-4">
                          <div className="border rounded-3 p-3 bg-light bg-opacity-50 h-100">
                            <div className="text-muted small mb-1">مانده پایان دوره</div>
                            <div className="fw-semibold">
                              <AmountDisplay value={ledgerData.closingBalance} />
                            </div>
                          </div>
                        </div>
                        <div className="col-md-4">
                          <div className="border rounded-3 p-3 bg-light bg-opacity-50 h-100">
                            <div className="text-muted small mb-1">بازه</div>
                            <div className="fw-semibold small">
                              {ledgerData.fromLabel || formatJalaliDate(ledgerData.from)}
                              {' تا '}
                              {ledgerData.toLabel || formatJalaliDate(ledgerData.to)}
                            </div>
                          </div>
                        </div>
                      </div>

                      <div className="table-responsive border rounded-3">
                        <table className="table table-sm table-hover align-middle mb-0">
                          <thead className="table-light">
                            <tr>
                              <th>تاریخ</th>
                              <th>شماره سند</th>
                              <th>شرح</th>
                              <th className="text-end">بدهکار</th>
                              <th className="text-end">بستانکار</th>
                              <th className="text-end">مانده</th>
                            </tr>
                          </thead>
                          <tbody>
                            <tr className="table-light">
                              <td colSpan={3} className="fw-semibold">
                                مانده اول دوره
                              </td>
                              <td className="text-end font-monospace">
                                <AmountDisplay value={ledgerData.openingDebit} />
                              </td>
                              <td className="text-end font-monospace">
                                <AmountDisplay value={ledgerData.openingCredit} />
                              </td>
                              <td className="text-end font-monospace fw-semibold">
                                <AmountDisplay value={ledgerData.openingBalance} />
                              </td>
                            </tr>
                            {(ledgerData.lines ?? []).map((line) => (
                              <tr key={line.journalLineId}>
                                <td>{formatJalaliDate(line.entryDate)}</td>
                                <td className="font-monospace">{line.entryNumber}</td>
                                <td>
                                  {line.lineDescription ||
                                    line.entryDescription ||
                                    '—'}
                                </td>
                                <td className="text-end font-monospace">
                                  <AmountDisplay value={line.debitInBase} />
                                </td>
                                <td className="text-end font-monospace">
                                  <AmountDisplay value={line.creditInBase} />
                                </td>
                                <td className="text-end font-monospace">
                                  <AmountDisplay value={line.runningBalance} />
                                </td>
                              </tr>
                            ))}
                            {(ledgerData.lines ?? []).length === 0 && (
                              <tr>
                                <td
                                  colSpan={6}
                                  className="text-center text-muted py-3"
                                >
                                  در این بازه گردشی ثبت نشده است.
                                </td>
                              </tr>
                            )}
                          </tbody>
                          <tfoot className="table-light">
                            <tr>
                              <td colSpan={3} className="fw-semibold">
                                مانده پایان دوره
                              </td>
                              <td colSpan={2} />
                              <td className="text-end fw-semibold font-monospace">
                                <AmountDisplay value={ledgerData.closingBalance} />
                              </td>
                            </tr>
                          </tfoot>
                        </table>
                      </div>
                    </>
                  )}
                </div>
                <div className="modal-footer border-0 pt-0">
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    onClick={closeLedger}
                  >
                    بستن
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

export default AccountsPage
