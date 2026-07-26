import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import Icon from '../../components/common/Icon'
import JalaliDateField from '../../components/common/JalaliDateField'
import DataTable from '../../lib/dataTableSetup'
import { usePageCrud } from '../../permissions/usePageCrud'
import { fetchMeaurmentOptions, fetchProductOptions } from '../../services/productsApi'
import {
  fetchWarehouseOptions,
  warehouseTransfersApi,
} from '../../services/inventoryApi'
import { dataTableLanguage, formatAmount } from '../Transport/CrudTablePage'

const statusBadge = {
  Draft: '<span class="badge bg-secondary">پیش‌نویس</span>',
  Posted: '<span class="badge badge-active">ثبت‌شده</span>',
  Cancelled: '<span class="badge badge-inactive">لغو شده</span>',
}

const columns = [
  { data: 'code', title: 'شماره سند' },
  { data: 'fromWarehouseName', title: 'از انبار' },
  { data: 'toWarehouseName', title: 'به انبار' },
  { data: 'transferDate', title: 'تاریخ' },
  {
    data: 'status',
    title: 'وضعیت',
    className: 'text-center',
    render: (data) => statusBadge[data] ?? data,
  },
  {
    data: 'totalCostInBaseCurrency',
    title: 'بهای کل',
    className: 'text-end',
    render: (data) => formatAmount(data),
  },
  {
    data: 'linesCount',
    title: 'اقلام',
    orderable: false,
    className: 'text-center',
  },
]

const emptyLine = { productId: '', quantity: '', meaurmentId: '', notes: '' }

function WarehouseTransfersPage() {
  const { canCreate, canEdit } = usePageCrud('/inventory/transfers')
  const tableRef = useRef(null)
  const [loadError, setLoadError] = useState('')
  const [showCreate, setShowCreate] = useState(false)
  const [detail, setDetail] = useState(null)
  const [formError, setFormError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [warehouseOptions, setWarehouseOptions] = useState([])
  const [productOptions, setProductOptions] = useState([])
  const [meaurmentOptions, setMeaurmentOptions] = useState([])
  const [form, setForm] = useState({
    fromWarehouseId: '',
    toWarehouseId: '',
    transferDate: '',
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

  const openCreate = () => {
    setFormError('')
    setForm({
      fromWarehouseId: '',
      toWarehouseId: '',
      transferDate: new Date().toISOString().slice(0, 10),
      notes: '',
      lines: [
        {
          ...emptyLine,
          meaurmentId: meaurmentOptions[0]?.value ?? '',
        },
      ],
    })
    setShowCreate(true)
  }

  const closeModals = () => {
    setShowCreate(false)
    setDetail(null)
    setFormError('')
    setSubmitting(false)
  }

  const openDetail = async (row) => {
    setFormError('')
    try {
      const data = await warehouseTransfersApi.getById(row.warehouseTransferId)
      setDetail(data)
    } catch (error) {
      setLoadError(error.message)
    }
  }

  const handlePost = async (transferId) => {
    setSubmitting(true)
    setFormError('')
    try {
      await warehouseTransfersApi.post(transferId)
      closeModals()
      reloadTable()
    } catch (error) {
      setFormError(error.message)
      setSubmitting(false)
    }
  }

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
        { ...emptyLine, meaurmentId: meaurmentOptions[0]?.value ?? '' },
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
    setSubmitting(true)
    setFormError('')

    if (form.fromWarehouseId === form.toWarehouseId) {
      setFormError('انبار مبدأ و مقصد نمی‌توانند یکسان باشند.')
      setSubmitting(false)
      return
    }

    const payload = {
      fromWarehouseId: Number(form.fromWarehouseId),
      toWarehouseId: Number(form.toWarehouseId),
      transferDate: form.transferDate || null,
      notes: form.notes.trim() || null,
      lines: form.lines
        .filter((line) => line.productId)
        .map((line) => ({
          productId: Number(line.productId),
          quantity: Number(line.quantity),
          meaurmentId: Number(line.meaurmentId),
          notes: line.notes.trim() || null,
        })),
    }

    try {
      await warehouseTransfersApi.create(payload)
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
      ajax: warehouseTransfersApi.createDataTableAjax(setLoadError),
      paging: true,
      searching: true,
      ordering: true,
      info: true,
      scrollX: true,
      autoWidth: false,
      responsive: true,
      stripeClasses: ['odd', 'even'],
      order: [[3, 'desc']],
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
            className="btn btn-sm btn-outline-secondary"
            title="جزئیات"
            onClick={() => openDetail(row)}
          >
            <Icon name="eye" size={14} />
          </button>
        </div>
      ),
    }),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [actionsIndex],
  )

  return (
    <div className="card border-0 shadow-sm">
      <div className="card-body">
        <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
          <h2 className="card-title mb-0">انتقال بین انبارها</h2>
          {canCreate && (
            <button type="button" className="btn btn-accent" onClick={openCreate}>
              <Icon name="plus" size={16} className="me-1" />
              <span>انتقال جدید</span>
            </button>
          )}
        </div>

        {loadError && <div className="alert alert-danger py-2">{loadError}</div>}

        <DataTable
          ref={tableRef}
          options={tableOptions}
          actionSlots={actionSlots}
          className="table table-hover w-100"
        />
      </div>

      {showCreate && (
        <>
          <div className="modal-backdrop show users-modal-backdrop" onClick={closeModals} />
          <div className="modal show d-block users-modal" tabIndex="-1" role="dialog">
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable modal-xl">
              <div className="modal-content">
                <form onSubmit={handleSubmit}>
                  <div className="modal-header">
                    <h5 className="modal-title">انتقال جدید</h5>
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
                        <label className="form-label">از انبار</label>
                        <select
                          className="form-select"
                          required
                          value={form.fromWarehouseId}
                          onChange={(e) =>
                            setForm((prev) => ({
                              ...prev,
                              fromWarehouseId: e.target.value,
                            }))
                          }
                        >
                          <option value="">انتخاب کنید</option>
                          {warehouseOptions.map((w) => (
                            <option key={w.value} value={w.value}>
                              {w.label}
                            </option>
                          ))}
                        </select>
                      </div>
                      <div className="col-md-4">
                        <label className="form-label">به انبار</label>
                        <select
                          className="form-select"
                          required
                          value={form.toWarehouseId}
                          onChange={(e) =>
                            setForm((prev) => ({
                              ...prev,
                              toWarehouseId: e.target.value,
                            }))
                          }
                        >
                          <option value="">انتخاب کنید</option>
                          {warehouseOptions.map((w) => (
                            <option key={w.value} value={w.value}>
                              {w.label}
                            </option>
                          ))}
                        </select>
                      </div>
                      <div className="col-md-4">
                        <label className="form-label">تاریخ</label>
                        <JalaliDateField
                          value={form.transferDate}
                          onChange={(value) =>
                            setForm((prev) => ({ ...prev, transferDate: value }))
                          }
                        />
                      </div>
                      <div className="col-12">
                        <label className="form-label">یادداشت</label>
                        <textarea
                          className="form-control"
                          rows={2}
                          value={form.notes}
                          onChange={(e) =>
                            setForm((prev) => ({ ...prev, notes: e.target.value }))
                          }
                        />
                      </div>
                    </div>

                    <div className="d-flex justify-content-between align-items-center mb-2">
                      <strong>اقلام</strong>
                      <button
                        type="button"
                        className="btn btn-sm btn-outline-secondary"
                        onClick={addLine}
                      >
                        ردیف جدید
                      </button>
                    </div>

                    {form.lines.map((line, index) => (
                      <div className="row g-2 mb-2 align-items-end" key={index}>
                        <div className="col-md-4">
                          <label className="form-label">محصول</label>
                          <select
                            className="form-select"
                            required
                            value={line.productId}
                            onChange={(e) =>
                              updateLine(index, { productId: e.target.value })
                            }
                          >
                            <option value="">انتخاب کنید</option>
                            {productOptions.map((p) => (
                              <option key={p.value} value={p.value}>
                                {p.label}
                              </option>
                            ))}
                          </select>
                        </div>
                        <div className="col-md-2">
                          <label className="form-label">مقدار</label>
                          <input
                            type="number"
                            step="any"
                            min="0"
                            className="form-control"
                            required
                            value={line.quantity}
                            onChange={(e) =>
                              updateLine(index, { quantity: e.target.value })
                            }
                          />
                        </div>
                        <div className="col-md-3">
                          <label className="form-label">واحد</label>
                          <select
                            className="form-select"
                            required
                            value={line.meaurmentId}
                            onChange={(e) =>
                              updateLine(index, { meaurmentId: e.target.value })
                            }
                          >
                            <option value="">انتخاب کنید</option>
                            {meaurmentOptions.map((m) => (
                              <option key={m.value} value={m.value}>
                                {m.label}
                              </option>
                            ))}
                          </select>
                        </div>
                        <div className="col-md-2">
                          <label className="form-label">یادداشت</label>
                          <input
                            type="text"
                            className="form-control"
                            value={line.notes}
                            onChange={(e) =>
                              updateLine(index, { notes: e.target.value })
                            }
                          />
                        </div>
                        <div className="col-md-1">
                          <button
                            type="button"
                            className="btn btn-outline-danger w-100"
                            disabled={form.lines.length <= 1}
                            onClick={() => removeLine(index)}
                          >
                            ×
                          </button>
                        </div>
                      </div>
                    ))}
                  </div>
                  <div className="modal-footer">
                    <button
                      type="button"
                      className="btn btn-outline-secondary"
                      onClick={closeModals}
                    >
                      انصراف
                    </button>
                    <button
                      type="submit"
                      className="btn btn-accent"
                      disabled={submitting}
                    >
                      {submitting ? 'در حال ذخیره...' : 'ذخیره پیش‌نویس'}
                    </button>
                  </div>
                </form>
              </div>
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
                  <h5 className="modal-title">جزئیات انتقال {detail.code}</h5>
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
                      <strong>از:</strong> {detail.fromWarehouseName}
                    </div>
                    <div className="col-md-3">
                      <strong>به:</strong> {detail.toWarehouseName}
                    </div>
                    <div className="col-md-3">
                      <strong>تاریخ:</strong>{' '}
                      {String(detail.transferDate).slice(0, 10)}
                    </div>
                    <div className="col-md-3">
                      <strong>وضعیت:</strong> {detail.status}
                    </div>
                    <div className="col-md-3">
                      <strong>بهای کل:</strong>{' '}
                      {formatAmount(detail.totalCostInBaseCurrency)}
                    </div>
                    <div className="col-md-3">
                      <strong>سند دفتر:</strong>{' '}
                      {detail.journalEntryId ? `#${detail.journalEntryId}` : '—'}
                    </div>
                    <div className="col-md-6">
                      <strong>یادداشت:</strong> {detail.notes || '—'}
                    </div>
                  </div>

                  <div className="table-responsive">
                    <table className="table table-sm table-hover">
                      <thead>
                        <tr>
                          <th>محصول</th>
                          <th>مقدار</th>
                          <th>معادل (kg)</th>
                          <th>بهای واحد</th>
                          <th>بهای کل</th>
                        </tr>
                      </thead>
                      <tbody>
                        {(detail.lines ?? []).map((line) => (
                          <tr key={line.warehouseTransferLineId}>
                            <td>
                              {line.productCode} — {line.productName}
                            </td>
                            <td>
                              {formatAmount(line.quantity)} {line.meaurmentName}
                            </td>
                            <td>{formatAmount(line.quantityInBase)}</td>
                            <td>{formatAmount(line.unitCostInBase)}</td>
                            <td>{formatAmount(line.lineCostInBase)}</td>
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
                      onClick={() => handlePost(detail.warehouseTransferId)}
                    >
                      {submitting
                        ? 'در حال ثبت...'
                        : 'ثبت نهایی (موجودی + سند حسابداری)'}
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

export default WarehouseTransfersPage
