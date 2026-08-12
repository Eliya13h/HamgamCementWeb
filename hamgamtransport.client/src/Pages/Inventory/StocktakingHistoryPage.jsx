import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import Icon from '../../components/common/Icon'
import JalaliDateField from '../../components/common/JalaliDateField'
import {
  useModalKeyboardShortcuts,
  usePageCreateShortcut,
  useModalAutoFocus,
} from '../../hooks/useModalKeyboardShortcuts'
import { showAppToast } from '../../lib/appToast'
import DataTable from '../../lib/dataTableSetup'
import { persianValidity, validateFormPersian } from '../../lib/persianFormValidity'
import { usePageCrud } from '../../permissions/usePageCrud'
import { fetchMeaurmentOptions, fetchProductOptions } from '../../services/productsApi'
import {
  fetchWarehouseOptions,
  stocktakingsApi,
} from '../../services/inventoryApi'
import { dataTableLanguage, formatAmount } from '../../components/common/CrudTablePage'

const statusBadge = {
  Draft: '<span class="badge bg-secondary">پیش‌نویس</span>',
  Confirmed: '<span class="badge badge-active">تأیید شده</span>',
  Cancelled: '<span class="badge badge-inactive">لغو شده</span>',
}

const columns = [
  { data: 'code', title: 'شماره سند' },
  { data: 'warehouseName', title: 'انبار' },
  { data: 'stocktakingDate', title: 'تاریخ' },
  {
    data: 'status',
    title: 'وضعیت',
    className: 'text-center',
    render: (data) => statusBadge[data] ?? data,
  },
  {
    data: 'linesCount',
    title: 'تعداد اقلام',
    orderable: false,
    className: 'text-center',
  },
  { data: 'notes', title: 'یادداشت', orderable: false },
]

const emptyLine = { productId: '', countedQuantity: '', countedMeaurmentId: '', notes: '' }

function StocktakingHistoryPage() {
  const { canCreate, canEdit } = usePageCrud('/inventory/stocktaking')
  const tableRef = useRef(null)
  const formRef = useRef(null)
  const [loadError, setLoadError] = useState('')
  const [showCreate, setShowCreate] = useState(false)
  const [detail, setDetail] = useState(null)
  const [formError, setFormError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [warehouseOptions, setWarehouseOptions] = useState([])
  const [productOptions, setProductOptions] = useState([])
  const [meaurmentOptions, setMeaurmentOptions] = useState([])
  const [form, setForm] = useState({
    warehouseId: '',
    stocktakingDate: '',
    notes: '',
    lines: [{ ...emptyLine }],
  })

  useEffect(() => {
    let cancelled = false
    async function loadOptions() {
      try {
        const [warehouses, products, meaurments] = await Promise.all([
          fetchWarehouseOptions(),
          fetchProductOptions(),
          fetchMeaurmentOptions(),
        ])
        if (!cancelled) {
          setWarehouseOptions(warehouses ?? [])
          setProductOptions(products ?? [])
          setMeaurmentOptions(meaurments ?? [])
        }
      } catch {
        // ignore
      }
    }
    loadOptions()
    return () => {
      cancelled = true
    }
  }, [])

  const reloadTable = useCallback(() => {
    tableRef.current?.dt()?.ajax.reload(null, false)
  }, [])

  const openCreate = useCallback(() => {
    setFormError('')
    setDetail(null)
    setForm({
      warehouseId: '',
      stocktakingDate: new Date().toISOString().slice(0, 10),
      notes: '',
      lines: [
        {
          ...emptyLine,
          countedMeaurmentId: meaurmentOptions[0]?.value ?? '',
        },
      ],
    })
    setShowCreate(true)
  }, [meaurmentOptions])

  const closeModals = useCallback(() => {
    setShowCreate(false)
    setDetail(null)
    setFormError('')
    setSubmitting(false)
  }, [])

  const openDetail = async (row) => {
    setFormError('')
    setShowCreate(false)
    try {
      const data = await stocktakingsApi.getById(row.stocktakingId)
      setDetail(data)
    } catch (error) {
      setLoadError(error.message)
    }
  }

  const handleConfirm = useCallback(async (stocktakingId) => {
    setSubmitting(true)
    setFormError('')
    try {
      await stocktakingsApi.confirm(stocktakingId)
      closeModals()
      reloadTable()
    } catch (error) {
      setFormError(error.message)
      setSubmitting(false)
    }
  }, [closeModals, reloadTable])

  const triggerCreateSave = useCallback(() => {
    if (!submitting) formRef.current?.requestSubmit()
  }, [submitting])

  const triggerConfirm = useCallback(() => {
    if (!submitting && detail?.status === 'Draft' && canEdit) {
      handleConfirm(detail.stocktakingId)
    }
  }, [submitting, detail, canEdit, handleConfirm])

  useModalKeyboardShortcuts({
    open: showCreate,
    onClose: closeModals,
    onSave: triggerCreateSave,
    formRef,
  })

  useModalKeyboardShortcuts({
    open: Boolean(detail),
    onClose: closeModals,
    onSave: detail?.status === 'Draft' && canEdit ? triggerConfirm : undefined,
  })

  usePageCreateShortcut({
    enabled: canCreate,
    onNew: openCreate,
    isBlocked: showCreate || Boolean(detail),
  })

  useModalAutoFocus({ open: showCreate, formRef })

  const updateLine = (index, patch) => {
    setForm((prev) => {
      const lines = [...prev.lines]
      lines[index] = { ...lines[index], ...patch }
      return { ...prev, lines }
    })
  }

  const addLine = () => {
    setForm((prev) => ({
      ...prev,
      lines: [
        ...prev.lines,
        { ...emptyLine, countedMeaurmentId: meaurmentOptions[0]?.value ?? '' },
      ],
    }))
  }

  const removeLine = (index) => {
    setForm((prev) => ({
      ...prev,
      lines: prev.lines.filter((_, i) => i !== index),
    }))
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    const formEl = event.currentTarget
    const message = validateFormPersian(formEl)
    if (message) {
      showAppToast(message)
      formEl.reportValidity()
      return
    }

    setSubmitting(true)
    setFormError('')

    const payload = {
      warehouseId: Number(form.warehouseId),
      stocktakingDate: form.stocktakingDate || null,
      notes: form.notes.trim() || null,
      lines: form.lines
        .filter((line) => line.productId)
        .map((line) => ({
          productId: Number(line.productId),
          countedQuantity: Number(line.countedQuantity),
          countedMeaurmentId: Number(line.countedMeaurmentId),
          notes: line.notes.trim() || null,
        })),
    }

    try {
      await stocktakingsApi.create(payload)
      closeModals()
      reloadTable()
    } catch (error) {
      setFormError(error.message)
      setSubmitting(false)
    }
  }

  const actionsIndex = columns.length + 1

  const tableOptions = useMemo(
    () => ({
      processing: true,
      serverSide: true,
      ajax: stocktakingsApi.createDataTableAjax(setLoadError),
      paging: true,
      searching: true,
      ordering: true,
      info: true,
      scrollX: false,
      autoWidth: false,
      responsive: true,
      stripeClasses: ['odd', 'even'],
      order: [[2, 'desc']],
      pageLength: 15,
      lengthMenu: [10, 15, 25, 50, 100],
      language: dataTableLanguage,
      layout: {
        topStart: {
          search: { placeholder: 'جستجو...' },
          pageLength: { menu: [10, 15, 25, 50, 100] },
        },
        topEnd: null,

        bottomStart: 'info',
        bottomEnd: { paging: { firstLast: true, previousNext: true, numbers: 5 } },
      },
      columns: [
        { data: 'rowNumber', name: 'rowNumber' },
        ...columns.map((col) => ({
          data: col.data,
          name: col.data,
          render: col.render,
        })),
        { data: null, name: 'actions', defaultContent: '' },
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
          targets: actionsIndex,
          orderable: false,
          searchable: false,
          className: 'text-center all dt-actions-col',
          width: '90px',
        },
      ],
    }),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [],
  )

  const actionSlots = useMemo(
    () => ({
      [actionsIndex]: (_data, _type, row) => (
        <div className="dt-actions">
          <button
            type="button"
            className="dt-action-btn"
            title="جزئیات"
            onClick={() => openDetail(row)}
          >
            <Icon name="eye" />
          </button>
        </div>
      ),
    }),
    [actionsIndex],
  )

  return (
    <div className="users-page">
      <div className="content-card card border-0 h-100">
        <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between gap-3 flex-wrap">
          <h2 className="card-title mb-0">سابقه انبارگردانی</h2>
          {canCreate && (
            <button
              type="button"
              className="btn btn-sm btn-accent btn-users-new d-inline-flex align-items-center gap-2"
              title="انبارگردانی جدید (Ctrl+N)"
              onClick={openCreate}
            >
              <Icon name="plus" />
              <span>انبارگردانی جدید</span>
            </button>
          )}
        </div>

        <div className="card-body card-body-table">
          {loadError && (
            <div className="alert alert-danger py-2 users-load-error mb-0">
              {loadError}
            </div>
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
                  {columns.map((col) => (
                    <th key={col.data}>{col.title}</th>
                  ))}
                  <th>عملیات</th>
                </tr>
              </thead>
            </DataTable>
          </div>
        </div>
      </div>

      {showCreate && (
        <>
          <div className="modal-backdrop show users-modal-backdrop" onClick={closeModals} />
          <div className="modal show d-block users-modal" tabIndex="-1" role="dialog">
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable modal-xl">
              <form ref={formRef} className="modal-content" onSubmit={handleSubmit} noValidate>
                <div className="modal-header">
                  <h5 className="modal-title">انبارگردانی جدید</h5>
                  <button
                    type="button"
                    className="btn-close"
                    aria-label="بستن"
                    onClick={closeModals}
                  />
                </div>
                <div className="modal-body">
                  {formError && (
                    <div className="alert alert-danger py-2">{formError}</div>
                  )}
                  <div className="row g-3 mb-3">
                    <div className="col-md-4">
                      <label className="form-label">انبار</label>
                      <select
                        className="form-select"
                        value={form.warehouseId}
                        required
                        {...persianValidity('لطفاً انبار را انتخاب کنید.')}
                        onChange={(e) =>
                          setForm({ ...form, warehouseId: e.target.value })
                        }
                      >
                        <option value="">انتخاب کنید...</option>
                        {warehouseOptions.map((option) => (
                          <option key={option.value} value={option.value}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div className="col-md-4">
                      <label className="form-label">تاریخ</label>
                      <JalaliDateField
                        value={form.stocktakingDate}
                        onChange={(next) =>
                          setForm({ ...form, stocktakingDate: next })
                        }
                      />
                    </div>
                    <div className="col-md-4">
                      <label className="form-label">یادداشت</label>
                      <input
                        className="form-control"
                        value={form.notes}
                        onChange={(e) => setForm({ ...form, notes: e.target.value })}
                      />
                    </div>
                  </div>

                  <div className="d-flex justify-content-between align-items-center mb-2">
                    <h6 className="mb-0">ردیف‌های شمارش</h6>
                    <button
                      type="button"
                      className="btn btn-sm btn-outline-primary"
                      onClick={addLine}
                    >
                      افزودن ردیف
                    </button>
                  </div>

                  <div className="table-responsive">
                    <table className="table table-sm align-middle">
                      <thead>
                        <tr>
                          <th>محصول</th>
                          <th>مقدار شمارش</th>
                          <th>واحد</th>
                          <th>یادداشت</th>
                          <th />
                        </tr>
                      </thead>
                      <tbody>
                        {form.lines.map((line, index) => (
                          <tr key={index}>
                            <td>
                              <select
                                className="form-select form-select-sm"
                                value={line.productId}
                                required
                                {...persianValidity('لطفاً محصول را انتخاب کنید.')}
                                onChange={(e) =>
                                  updateLine(index, { productId: e.target.value })
                                }
                              >
                                <option value="">انتخاب...</option>
                                {productOptions.map((option) => (
                                  <option key={option.value} value={option.value}>
                                    {option.label}
                                  </option>
                                ))}
                              </select>
                            </td>
                            <td>
                              <input
                                type="number"
                                className="form-control form-control-sm"
                                step="any"
                                required
                                {...persianValidity('لطفاً مقدار شمارش را وارد کنید.')}
                                value={line.countedQuantity}
                                onChange={(e) =>
                                  updateLine(index, {
                                    countedQuantity: e.target.value,
                                  })
                                }
                              />
                            </td>
                            <td>
                              <select
                                className="form-select form-select-sm"
                                value={line.countedMeaurmentId}
                                required
                                {...persianValidity('لطفاً واحد را انتخاب کنید.')}
                                onChange={(e) =>
                                  updateLine(index, {
                                    countedMeaurmentId: e.target.value,
                                  })
                                }
                              >
                                {meaurmentOptions.map((option) => (
                                  <option key={option.value} value={option.value}>
                                    {option.label}
                                  </option>
                                ))}
                              </select>
                            </td>
                            <td>
                              <input
                                className="form-control form-control-sm"
                                value={line.notes}
                                onChange={(e) =>
                                  updateLine(index, { notes: e.target.value })
                                }
                              />
                            </td>
                            <td>
                              {form.lines.length > 1 && (
                                <button
                                  type="button"
                                  className="btn btn-sm btn-outline-danger"
                                  onClick={() => removeLine(index)}
                                >
                                  حذف
                                </button>
                              )}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
                <div className="modal-footer">
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    onClick={closeModals}
                  >
                    انصراف
                  </button>
                  <button type="submit" className="btn btn-accent" disabled={submitting}>
                    {submitting ? 'در حال ذخیره...' : 'ثبت پیش‌نویس'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </>
      )}

      {detail && (
        <>
          <div className="modal-backdrop show users-modal-backdrop" onClick={closeModals} />
          <div className="modal show d-block users-modal" tabIndex="-1" role="dialog">
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable modal-xl">
              <div className="modal-content">
                <div className="modal-header">
                  <h5 className="modal-title">
                    جزئیات انبارگردانی {detail.code}
                  </h5>
                  <button
                    type="button"
                    className="btn-close"
                    aria-label="بستن"
                    onClick={closeModals}
                  />
                </div>
                <div className="modal-body">
                  {formError && (
                    <div className="alert alert-danger py-2">{formError}</div>
                  )}
                  <div className="row g-2 mb-3 small">
                    <div className="col-md-3">
                      <strong>انبار:</strong> {detail.warehouseName}
                    </div>
                    <div className="col-md-3">
                      <strong>تاریخ:</strong>{' '}
                      {String(detail.stocktakingDate).slice(0, 10)}
                    </div>
                    <div className="col-md-3">
                      <strong>وضعیت:</strong> {detail.status}
                    </div>
                    <div className="col-md-3">
                      <strong>سند دفتر:</strong>{' '}
                      {detail.journalEntryId ? `#${detail.journalEntryId}` : '—'}
                    </div>
                    <div className="col-md-12">
                      <strong>یادداشت:</strong> {detail.notes || '—'}
                    </div>
                  </div>

                  <div className="table-responsive">
                    <table className="table table-sm table-hover">
                      <thead>
                        <tr>
                          <th>محصول</th>
                          <th>موجودی سیستم (kg)</th>
                          <th>شمارش</th>
                          <th>معادل (kg)</th>
                          <th>اختلاف (kg)</th>
                          <th>بهای تعدیل</th>
                        </tr>
                      </thead>
                      <tbody>
                        {(detail.lines ?? []).map((line) => (
                          <tr key={line.stocktakingLineId}>
                            <td>
                              {line.productCode} — {line.productName}
                            </td>
                            <td>{formatAmount(line.systemQuantityInBase)}</td>
                            <td>
                              {formatAmount(line.countedQuantity)}{' '}
                              {line.countedMeaurmentName}
                            </td>
                            <td>{formatAmount(line.countedQuantityInBase)}</td>
                            <td
                              className={
                                line.differenceInBase < 0
                                  ? 'text-danger'
                                  : line.differenceInBase > 0
                                    ? 'text-success'
                                    : ''
                              }
                            >
                              {formatAmount(line.differenceInBase)}
                            </td>
                            <td>{formatAmount(line.adjustmentCostInBase ?? 0)}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
                <div className="modal-footer">
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    onClick={closeModals}
                  >
                    بستن
                  </button>
                  {canEdit && detail.status === 'Draft' && (
                    <button
                      type="button"
                      className="btn btn-accent"
                      disabled={submitting}
                      onClick={() => handleConfirm(detail.stocktakingId)}
                    >
                      {submitting
                        ? 'در حال تأیید...'
                        : 'تأیید موجودی و ثبت سند حسابداری'}
                    </button>
                  )}
                </div>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  )
}

export default StocktakingHistoryPage
