import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import JalaliDateField from '../../components/common/JalaliDateField'
import DataTable from '../../lib/dataTableSetup'
import {
  createServerSideTableOptions,
  formatAmount,
} from '../../lib/dataTableOptions'
import {
  formatJalaliDate,
  todayGregorianIso,
  toLatinIsoDate,
} from '../../lib/afghanSolarCalendar'
import {
  attachmentsApi,
  costCentersApi,
  fetchAccountTree,
  journalEntriesApi,
} from '../../services/ledgerApi'

/** صفر سمت خالی خط سند را خالی نشان بده */
function formatLineAmount(value) {
  const num = Number(value)
  if (!Number.isFinite(num) || num === 0) return '—'
  return formatAmount(num)
}

function emptyLine() {
  return { accountId: '', debit: '', credit: '', description: '', costCenterId: '' }
}

function JournalEntriesPage() {
  const tableRef = useRef(null)
  const [loadError, setLoadError] = useState('')
  const [selected, setSelected] = useState(null)
  const [detailError, setDetailError] = useState('')
  const [detailLoading, setDetailLoading] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [reversing, setReversing] = useState(false)
  const [attachments, setAttachments] = useState([])
  const [attachmentsLoading, setAttachmentsLoading] = useState(false)
  const [attachmentUploading, setAttachmentUploading] = useState(false)

  const [showCreate, setShowCreate] = useState(false)
  const [postableAccounts, setPostableAccounts] = useState([])
  const [accountsLoading, setAccountsLoading] = useState(false)
  const [costCenters, setCostCenters] = useState([])
  const [createForm, setCreateForm] = useState({
    entryDate: todayGregorianIso(),
    description: '',
    lines: [emptyLine(), emptyLine()],
  })
  const [createError, setCreateError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const reloadTable = useCallback(() => {
    tableRef.current?.dt()?.ajax.reload(null, false)
  }, [])

  const closeDetail = () => {
    setSelected(null)
    setDetailError('')
    setDetailLoading(false)
    setDeleting(false)
    setReversing(false)
    setAttachments([])
  }

  const openDetail = async (journalEntryId) => {
    setDetailLoading(true)
    setDetailError('')
    setSelected(null)
    try {
      const detail = await journalEntriesApi.get(journalEntryId)
      setSelected(detail)
      setLoadError('')
      setAttachmentsLoading(true)
      attachmentsApi
        .list('JournalEntry', journalEntryId)
        .then((items) => setAttachments(items ?? []))
        .catch(() => setAttachments([]))
        .finally(() => setAttachmentsLoading(false))
    } catch (err) {
      setDetailError(err.message || 'خطا در دریافت جزئیات سند')
    } finally {
      setDetailLoading(false)
    }
  }

  const handleDelete = async () => {
    if (!selected?.canDelete) return
    if (!window.confirm(`سند شماره ${selected.entryNumber} حذف شود؟`)) return
    setDeleting(true)
    setDetailError('')
    try {
      await journalEntriesApi.remove(selected.journalEntryId)
      closeDetail()
      reloadTable()
    } catch (err) {
      setDetailError(err.message || 'حذف سند با خطا مواجه شد.')
      setDeleting(false)
    }
  }

  const handleReverse = async () => {
    if (!selected?.canDelete) return
    if (!window.confirm(`برای سند شماره ${selected.entryNumber} سند معکوس ثبت شود؟`)) return
    setReversing(true)
    setDetailError('')
    try {
      await journalEntriesApi.reverse(selected.journalEntryId)
      closeDetail()
      reloadTable()
    } catch (err) {
      setDetailError(err.message || 'ثبت سند معکوس با خطا مواجه شد.')
      setReversing(false)
    }
  }

  const handleAttachmentUpload = async (event) => {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file || !selected) return
    setAttachmentUploading(true)
    setDetailError('')
    try {
      await attachmentsApi.upload('JournalEntry', selected.journalEntryId, file)
      setAttachments(await attachmentsApi.list('JournalEntry', selected.journalEntryId))
    } catch (err) {
      setDetailError(err.message || 'آپلود پیوست ناموفق بود.')
    } finally {
      setAttachmentUploading(false)
    }
  }

  const handleAttachmentDelete = async (attachmentId) => {
    try {
      await attachmentsApi.remove(attachmentId)
      setAttachments((prev) => prev.filter((item) => item.attachmentId !== attachmentId))
    } catch (err) {
      setDetailError(err.message || 'حذف پیوست ناموفق بود.')
    }
  }

  const openCreate = async () => {
    setShowCreate(true)
    setCreateError('')
    setCreateForm({
      entryDate: todayGregorianIso(),
      description: '',
      lines: [emptyLine(), emptyLine()],
    })
    setAccountsLoading(true)
    try {
      const [tree, centers] = await Promise.all([fetchAccountTree(), costCentersApi.options()])
      setPostableAccounts((tree ?? []).filter((a) => a.isPostable))
      setCostCenters(centers ?? [])
    } catch (err) {
      setCreateError(err.message || 'بارگذاری حساب‌ها ناموفق بود.')
      setPostableAccounts([])
    } finally {
      setAccountsLoading(false)
    }
  }

  const closeCreate = () => {
    setShowCreate(false)
    setCreateError('')
    setSubmitting(false)
  }

  const updateLine = (index, patch) => {
    setCreateForm((prev) => ({
      ...prev,
      lines: prev.lines.map((line, i) =>
        i === index ? { ...line, ...patch } : line,
      ),
    }))
  }

  const addLine = () => {
    setCreateForm((prev) => ({
      ...prev,
      lines: [...prev.lines, emptyLine()],
    }))
  }

  const removeLine = (index) => {
    setCreateForm((prev) => ({
      ...prev,
      lines:
        prev.lines.length <= 2
          ? prev.lines
          : prev.lines.filter((_, i) => i !== index),
    }))
  }

  const totals = useMemo(() => {
    return createForm.lines.reduce(
      (acc, line) => {
        acc.debit += Number(line.debit) || 0
        acc.credit += Number(line.credit) || 0
        return acc
      },
      { debit: 0, credit: 0 },
    )
  }, [createForm.lines])

  const validateCreate = () => {
    if (!createForm.entryDate) {
      return 'تاریخ سند الزامی است.'
    }
    if (createForm.lines.length < 2) {
      return 'سند باید حداقل دو ردیف داشته باشد.'
    }

    for (let i = 0; i < createForm.lines.length; i += 1) {
      const line = createForm.lines[i]
      const debit = Number(line.debit) || 0
      const credit = Number(line.credit) || 0
      if (!line.accountId) {
        return `ردیف ${i + 1}: انتخاب حساب الزامی است.`
      }
      if (debit < 0 || credit < 0) {
        return `ردیف ${i + 1}: مبلغ نمی‌تواند منفی باشد.`
      }
      if ((debit > 0 && credit > 0) || (debit === 0 && credit === 0)) {
        return `ردیف ${i + 1}: باید فقط بدهکار یا فقط بستانکار باشد.`
      }
    }

    if (Math.abs(totals.debit - totals.credit) > 0.0001) {
      return 'جمع بدهکار و بستانکار باید برابر باشد.'
    }

    return ''
  }

  const handleCreateSubmit = async (event) => {
    event.preventDefault()
    const validationError = validateCreate()
    if (validationError) {
      setCreateError(validationError)
      return
    }

    setSubmitting(true)
    setCreateError('')
    try {
      await journalEntriesApi.create({
        entryDate: toLatinIsoDate(createForm.entryDate),
        description: createForm.description.trim() || null,
        lines: createForm.lines.map((line) => ({
          accountId: Number(line.accountId),
          debit: Number(line.debit) || 0,
          credit: Number(line.credit) || 0,
          description: line.description.trim() || null,
          costCenterId: line.costCenterId ? Number(line.costCenterId) : null,
        })),
      })
      closeCreate()
      reloadTable()
    } catch (err) {
      setCreateError(err.message || 'ثبت سند با خطا مواجه شد.')
    } finally {
      setSubmitting(false)
    }
  }

  const tableOptions = useMemo(
    () =>
      createServerSideTableOptions({
        ajax: journalEntriesApi.createDataTableAjax(setLoadError),
        searching: true,
        order: [[2, 'desc']],
        columns: [
          { data: 'rowNumber', name: 'rowNumber', orderable: false },
          { data: 'entryNumber', name: 'entryNumber' },
          {
            data: 'entryDate',
            name: 'entryDate',
            render: (data, type) =>
              type === 'display' ? formatJalaliDate(data) : data,
          },
          { data: 'description', name: 'description', orderable: false },
          { data: 'sourceLabel', name: 'sourceLabel', orderable: false },
          {
            data: 'totalDebitInBaseCurrency',
            name: 'totalDebitInBaseCurrency',
            className: 'text-end',
            render: (data, type) =>
              type === 'display' ? formatAmount(data) : data,
          },
          {
            data: 'totalCreditInBaseCurrency',
            name: 'totalCreditInBaseCurrency',
            className: 'text-end',
            render: (data, type) =>
              type === 'display' ? formatAmount(data) : data,
          },
          {
            data: null,
            name: 'actions',
            orderable: false,
            searchable: false,
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
          {
            targets: -1,
            orderable: false,
            searchable: false,
            className: 'text-center',
          },
        ],
      }),
    [],
  )

  const actionSlots = useMemo(
    () => ({
      7: (_data, _type, row) => (
        <button
          type="button"
          className="btn btn-sm btn-outline-primary"
          onClick={() => openDetail(row.journalEntryId)}
        >
          مشاهده
        </button>
      ),
    }),
    [],
  )

  const showModal = detailLoading || selected || detailError

  useEffect(() => {
    if (!showCreate) return undefined
    const onKey = (e) => {
      if (e.key === 'Escape') closeCreate()
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [showCreate])

  return (
    <div className="users-page">
      <div className="content-card card border-0">
        <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between gap-3 flex-wrap">
          <h2 className="card-title mb-0">اسناد دفترروزنامه</h2>
          <button
            type="button"
            className="btn btn-sm btn-accent btn-users-new"
            onClick={openCreate}
          >
            سند دستی جدید
          </button>
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
              slots={actionSlots}
            >
              <thead>
                <tr>
                  <th>#</th>
                  <th>شماره</th>
                  <th>تاریخ</th>
                  <th>شرح</th>
                  <th>منبع</th>
                  <th>بدهکار</th>
                  <th>بستانکار</th>
                  <th>عملیات</th>
                </tr>
              </thead>
            </DataTable>
          </div>
        </div>
      </div>

      {showCreate && (
        <>
          <div
            className="modal-backdrop show users-modal-backdrop"
            onClick={closeCreate}
          />
          <div
            className="modal show d-block users-modal"
            tabIndex="-1"
            role="dialog"
            aria-modal="true"
            aria-labelledby="journal-entry-create-title"
          >
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable modal-xl">
              <div className="modal-content">
                <form onSubmit={handleCreateSubmit}>
                  <div className="modal-header border-0 pb-0">
                    <h5
                      className="modal-title"
                      id="journal-entry-create-title"
                    >
                      سند دستی جدید
                    </h5>
                    <button
                      type="button"
                      className="btn-close"
                      aria-label="بستن"
                      onClick={closeCreate}
                    />
                  </div>
                  <div className="modal-body pt-3">
                    {createError && (
                      <div className="alert alert-danger py-2">{createError}</div>
                    )}

                    <div className="row g-3 mb-3">
                      <div className="col-md-4">
                        <label className="form-label">تاریخ سند</label>
                        <JalaliDateField
                          value={createForm.entryDate}
                          onChange={(value) =>
                            setCreateForm((prev) => ({
                              ...prev,
                              entryDate: value,
                            }))
                          }
                          required
                        />
                      </div>
                      <div className="col-md-8">
                        <label className="form-label">شرح</label>
                        <input
                          type="text"
                          className="form-control"
                          value={createForm.description}
                          onChange={(e) =>
                            setCreateForm((prev) => ({
                              ...prev,
                              description: e.target.value,
                            }))
                          }
                          placeholder="شرح سند دستی"
                        />
                      </div>
                    </div>

                    <div className="d-flex align-items-center justify-content-between mb-2">
                      <h6 className="mb-0">خطوط سند</h6>
                      <button
                        type="button"
                        className="btn btn-sm btn-outline-primary"
                        onClick={addLine}
                      >
                        افزودن ردیف
                      </button>
                    </div>

                    {accountsLoading ? (
                      <div className="text-muted mb-3">
                        در حال بارگذاری حساب‌های قابل ثبت...
                      </div>
                    ) : null}

                    <div className="table-responsive border rounded-3 mb-3">
                      <table className="table table-sm align-middle mb-0">
                        <thead className="table-light">
                          <tr>
                            <th style={{ minWidth: 220 }}>حساب</th>
                            <th style={{ minWidth: 160 }}>مرکز هزینه</th>
                            <th style={{ width: 140 }}>بدهکار</th>
                            <th style={{ width: 140 }}>بستانکار</th>
                            <th>شرح ردیف</th>
                            <th style={{ width: 70 }} />
                          </tr>
                        </thead>
                        <tbody>
                          {createForm.lines.map((line, index) => (
                            <tr key={index}>
                              <td>
                                <select
                                  className="form-select form-select-sm"
                                  value={line.accountId}
                                  onChange={(e) =>
                                    updateLine(index, {
                                      accountId: e.target.value,
                                    })
                                  }
                                  required
                                >
                                  <option value="">انتخاب حساب...</option>
                                  {postableAccounts.map((acc) => (
                                    <option
                                      key={acc.accountId}
                                      value={acc.accountId}
                                    >
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
                                  {costCenters.map((center) => (
                                    <option key={center.value} value={center.value}>
                                      {center.label}
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
                                  value={line.debit}
                                  onChange={(e) =>
                                    updateLine(index, {
                                      debit: e.target.value,
                                      credit: e.target.value ? '' : line.credit,
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
                                  value={line.credit}
                                  onChange={(e) =>
                                    updateLine(index, {
                                      credit: e.target.value,
                                      debit: e.target.value ? '' : line.debit,
                                    })
                                  }
                                />
                              </td>
                              <td>
                                <input
                                  type="text"
                                  className="form-control form-control-sm"
                                  value={line.description}
                                  onChange={(e) =>
                                    updateLine(index, {
                                      description: e.target.value,
                                    })
                                  }
                                />
                              </td>
                              <td className="text-center">
                                <button
                                  type="button"
                                  className="btn btn-sm btn-outline-danger"
                                  onClick={() => removeLine(index)}
                                  disabled={createForm.lines.length <= 2}
                                  title="حذف ردیف"
                                >
                                  ×
                                </button>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                        <tfoot className="table-light">
                          <tr>
                            <td className="fw-semibold">جمع</td>
                            <td className="text-end fw-semibold font-monospace">
                              {formatAmount(totals.debit)}
                            </td>
                            <td className="text-end fw-semibold font-monospace">
                              {formatAmount(totals.credit)}
                            </td>
                            <td
                              colSpan={3}
                              className={
                                Math.abs(totals.debit - totals.credit) > 0.0001
                                  ? 'text-danger small'
                                  : 'text-success small'
                              }
                            >
                              {Math.abs(totals.debit - totals.credit) > 0.0001
                                ? 'نامتوازن'
                                : 'متوازن'}
                            </td>
                          </tr>
                        </tfoot>
                      </table>
                    </div>
                  </div>
                  <div className="modal-footer border-0 pt-0">
                    <button
                      type="button"
                      className="btn btn-outline-secondary"
                      onClick={closeCreate}
                      disabled={submitting}
                    >
                      انصراف
                    </button>
                    <button
                      type="submit"
                      className="btn btn-primary"
                      disabled={submitting || accountsLoading}
                    >
                      {submitting ? 'در حال ثبت...' : 'ثبت سند'}
                    </button>
                  </div>
                </form>
              </div>
            </div>
          </div>
        </>
      )}

      {showModal && (
        <>
          <div
            className="modal-backdrop show users-modal-backdrop"
            onClick={closeDetail}
          />
          <div
            className="modal show d-block users-modal"
            tabIndex="-1"
            role="dialog"
            aria-modal="true"
            aria-labelledby="journal-entry-detail-title"
          >
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable modal-xl">
              <div className="modal-content">
                <div className="modal-header border-0 pb-0">
                  <div>
                    <h5
                      className="modal-title mb-1"
                      id="journal-entry-detail-title"
                    >
                      {selected
                        ? `سند شماره ${selected.entryNumber}`
                        : 'جزئیات سند'}
                    </h5>
                    {selected?.sourceLabel && (
                      <span className="badge rounded-pill text-bg-light border">
                        {selected.sourceLabel}
                      </span>
                    )}
                  </div>
                  <button
                    type="button"
                    className="btn-close"
                    aria-label="بستن"
                    onClick={closeDetail}
                  />
                </div>

                <div className="modal-body pt-3">
                  {detailError && (
                    <div className="alert alert-danger py-2">{detailError}</div>
                  )}

                  {detailLoading && (
                    <div className="text-center text-muted py-5">
                      در حال بارگذاری جزئیات سند...
                    </div>
                  )}

                  {selected && !detailLoading && (
                    <>
                      <div className="row g-3 mb-4">
                        <div className="col-md-3 col-6">
                          <div className="border rounded-3 p-3 h-100 bg-light bg-opacity-50">
                            <div className="text-muted small mb-1">تاریخ</div>
                            <div className="fw-semibold">
                              {formatJalaliDate(selected.entryDate)}
                            </div>
                          </div>
                        </div>
                        <div className="col-md-3 col-6">
                          <div className="border rounded-3 p-3 h-100 bg-light bg-opacity-50">
                            <div className="text-muted small mb-1">منبع</div>
                            <div className="fw-semibold">
                              {selected.sourceLabel || '—'}
                            </div>
                          </div>
                        </div>
                        <div className="col-md-3 col-6">
                          <div className="border rounded-3 p-3 h-100 bg-light bg-opacity-50">
                            <div className="text-muted small mb-1">
                              جمع بدهکار
                            </div>
                            <div className="fw-semibold text-end font-monospace">
                              {formatAmount(selected.totalDebitInBaseCurrency)}
                            </div>
                          </div>
                        </div>
                        <div className="col-md-3 col-6">
                          <div className="border rounded-3 p-3 h-100 bg-light bg-opacity-50">
                            <div className="text-muted small mb-1">
                              جمع بستانکار
                            </div>
                            <div className="fw-semibold text-end font-monospace">
                              {formatAmount(selected.totalCreditInBaseCurrency)}
                            </div>
                          </div>
                        </div>
                        <div className="col-12">
                          <div className="border rounded-3 p-3 bg-light bg-opacity-50">
                            <div className="text-muted small mb-1">شرح سند</div>
                            <div>{selected.description || '—'}</div>
                          </div>
                        </div>
                      </div>

                      <div className="d-flex align-items-center justify-content-between mb-2">
                        <h6 className="mb-0">خطوط سند</h6>
                        <span className="text-muted small">
                          {(selected.lines ?? []).length} ردیف
                        </span>
                      </div>

                      <div className="table-responsive border rounded-3">
                        <table className="table table-sm table-hover align-middle mb-0">
                          <thead className="table-light">
                            <tr>
                              <th style={{ width: 48 }}>#</th>
                              <th style={{ width: 110 }}>کد حساب</th>
                              <th>حساب</th>
                              <th>شرح</th>
                              <th className="text-end" style={{ width: 140 }}>
                                بدهکار
                              </th>
                              <th className="text-end" style={{ width: 140 }}>
                                بستانکار
                              </th>
                            </tr>
                          </thead>
                          <tbody>
                            {(selected.lines ?? []).map((line) => (
                              <tr key={line.journalLineId}>
                                <td className="text-muted">{line.lineNo ?? line.lineNumber}</td>
                                <td className="font-monospace">
                                  {line.accountCode}
                                </td>
                                <td>{line.accountName}</td>
                                <td className="text-muted">
                                  {line.description ?? '—'}
                                </td>
                                <td className="text-end font-monospace">
                                  {formatLineAmount(line.debitInBaseCurrency)}
                                </td>
                                <td className="text-end font-monospace">
                                  {formatLineAmount(line.creditInBaseCurrency)}
                                </td>
                              </tr>
                            ))}
                            {(selected.lines ?? []).length === 0 && (
                              <tr>
                                <td
                                  colSpan={6}
                                  className="text-center text-muted py-4"
                                >
                                  خطی برای این سند ثبت نشده است.
                                </td>
                              </tr>
                            )}
                          </tbody>
                          {(selected.lines ?? []).length > 0 && (
                            <tfoot className="table-light">
                              <tr>
                                <td colSpan={4} className="fw-semibold">
                                  جمع
                                </td>
                                <td className="text-end fw-semibold font-monospace">
                                  {formatAmount(
                                    selected.totalDebitInBaseCurrency,
                                  )}
                                </td>
                                <td className="text-end fw-semibold font-monospace">
                                  {formatAmount(
                                    selected.totalCreditInBaseCurrency,
                                  )}
                                </td>
                              </tr>
                            </tfoot>
                          )}
                        </table>
                      </div>

                      <div className="mt-4">
                        <div className="d-flex align-items-center justify-content-between mb-2">
                          <h6 className="mb-0">پیوست‌ها</h6>
                          <label className="btn btn-sm btn-outline-primary mb-0">
                            {attachmentUploading ? 'در حال آپلود...' : 'افزودن پیوست'}
                            <input
                              type="file"
                              className="d-none"
                              onChange={handleAttachmentUpload}
                              disabled={attachmentUploading}
                            />
                          </label>
                        </div>
                        {attachmentsLoading ? (
                          <small className="text-muted">در حال بارگذاری پیوست‌ها...</small>
                        ) : attachments.length > 0 ? (
                          <ul className="list-group list-group-flush border rounded">
                            {attachments.map((attachment) => (
                              <li key={attachment.attachmentId} className="list-group-item d-flex justify-content-between align-items-center">
                                <a href={attachment.relativePath} target="_blank" rel="noreferrer">
                                  {attachment.fileName}
                                </a>
                                <button
                                  type="button"
                                  className="btn btn-sm btn-outline-danger"
                                  onClick={() => handleAttachmentDelete(attachment.attachmentId)}
                                >
                                  حذف
                                </button>
                              </li>
                            ))}
                          </ul>
                        ) : (
                          <small className="text-muted">پیوستی ثبت نشده است.</small>
                        )}
                      </div>
                    </>
                  )}
                </div>

                <div className="modal-footer border-0 pt-0 d-flex justify-content-between">
                  <div>
                    {selected?.canDelete && !detailLoading && (
                      <button
                        type="button"
                        className="btn btn-outline-warning me-2"
                        onClick={handleReverse}
                        disabled={reversing}
                      >
                        {reversing ? 'در حال ثبت...' : 'معکوس'}
                      </button>
                    )}
                    {selected?.canDelete && !detailLoading && (
                      <button
                        type="button"
                        className="btn btn-outline-danger"
                        onClick={handleDelete}
                        disabled={deleting}
                      >
                        {deleting ? 'در حال حذف...' : 'حذف سند'}
                      </button>
                    )}
                  </div>
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    onClick={closeDetail}
                    disabled={deleting}
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

export default JournalEntriesPage
