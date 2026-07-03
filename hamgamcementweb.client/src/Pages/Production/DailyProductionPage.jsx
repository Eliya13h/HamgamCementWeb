import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import Icon from '../../components/common/Icon'
import AmountField from '../../components/common/AmountField'
import JalaliDateField from '../../components/common/JalaliDateField'
import SearchableSelect from '../../components/common/SearchableSelect'
import DataTable from '../../lib/dataTableSetup'
import { usePageCrud } from '../../permissions/usePageCrud'
import { todayGregorianIso } from '../../lib/afghanSolarCalendar'
import {
  fetchProcessedWarehouseOptions,
  fetchProductionMaterialWarehouses,
} from '../../services/inventoryApi'
import { fetchMeaurmentOptions, fetchProductOptions } from '../../services/productsApi'
import {
  buildProductionBatchPayload,
  productionBatchesApi,
} from '../../services/productionApi'
import { dataTableLanguage, formatAmount, formatJalaliDate } from '../Transport/CrudTablePage'

const emptyInputLine = { warehouseId: '', productId: '', meaurmentId: '', quantity: '' }
const emptyOutputLine = { productId: '', meaurmentId: '', quantity: '' }

function DailyProductionPage() {
  const tableRef = useRef(null)
  const { canCreate, canEdit, canDelete } = usePageCrud('/production/daily')
  const [loadError, setLoadError] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editId, setEditId] = useState(null)
  const [viewPosted, setViewPosted] = useState(false)
  const [deleteRow, setDeleteRow] = useState(null)
  const [traceData, setTraceData] = useState(null)
  const [formError, setFormError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [materialWarehouses, setMaterialWarehouses] = useState([])
  const [processedWarehouses, setProcessedWarehouses] = useState([])
  const [products, setProducts] = useState([])
  const [meaurments, setMeaurments] = useState([])
  const [form, setForm] = useState({
    productionDate: '',
    outputWarehouseId: '',
    fixedCost: '',
    variableCost: '',
    description: '',
    inputLines: [{ ...emptyInputLine }],
    outputLines: [{ ...emptyOutputLine }],
  })

  useEffect(() => {
    fetchProductionMaterialWarehouses().then(setMaterialWarehouses).catch(() => setMaterialWarehouses([]))
    fetchProcessedWarehouseOptions().then(setProcessedWarehouses).catch(() => setProcessedWarehouses([]))
    fetchProductOptions().then(setProducts).catch(() => setProducts([]))
    fetchMeaurmentOptions().then(setMeaurments).catch(() => setMeaurments([]))
  }, [])

  const meaurmentsForProduct = useCallback(
    (productId) => {
      const product = products.find((p) => String(p.value) === String(productId))
      if (!product?.baseMeaurmentId) return meaurments
      return meaurments.filter(
        (m) => m.baseMeaurmentId === product.baseMeaurmentId || m.value === product.baseMeaurmentId,
      )
    },
    [products, meaurments],
  )

  const reloadTable = useCallback(() => {
    tableRef.current?.dt()?.ajax.reload(null, false)
  }, [])

  const closeModals = useCallback(() => {
    setShowForm(false)
    setEditId(null)
    setViewPosted(false)
    setDeleteRow(null)
    setTraceData(null)
    setFormError('')
    setSubmitting(false)
  }, [])

  const openCreate = useCallback(() => {
    setFormError('')
    setForm({
      productionDate: todayGregorianIso(),
      outputWarehouseId: '',
      fixedCost: '',
      variableCost: '',
      description: '',
      inputLines: [{ ...emptyInputLine }],
      outputLines: [{ ...emptyOutputLine }],
    })
    setEditId(null)
    setViewPosted(false)
    setShowForm(true)
  }, [])

  const openEdit = useCallback(async (row, readOnly = false) => {
    setFormError('')
    try {
      const batch = await productionBatchesApi.getById(row.productionBatchId)
      setForm({
        productionDate: String(batch.productionDate).slice(0, 10),
        outputWarehouseId: batch.outputWarehouseId,
        fixedCost: batch.fixedCost ?? '',
        variableCost: batch.variableCost ?? '',
        description: batch.description ?? '',
        inputLines: (batch.inputLines ?? []).map((line) => ({
          warehouseId: line.warehouseId,
          productId: line.productId,
          meaurmentId: line.meaurmentId,
          quantity: line.quantity,
        })),
        outputLines: (batch.outputLines ?? []).map((line) => ({
          productId: line.productId,
          meaurmentId: line.meaurmentId,
          quantity: line.quantity,
        })),
      })
      setEditId(batch.productionBatchId)
      setViewPosted(readOnly || batch.isPosted)
      setShowForm(true)
    } catch (error) {
      setLoadError(error.message)
    }
  }, [])

  const openTrace = useCallback(async (row) => {
    setFormError('')
    try {
      const trace = await productionBatchesApi.trace(row.productionBatchId)
      setTraceData(trace)
    } catch (error) {
      setLoadError(error.message)
    }
  }, [])

  const handleSubmit = async (event) => {
    event.preventDefault()
    if (viewPosted) return
    setSubmitting(true)
    setFormError('')
    try {
      const payload = buildProductionBatchPayload(form)
      if (editId) {
        await productionBatchesApi.update(editId, payload)
      } else {
        await productionBatchesApi.create(payload)
      }
      closeModals()
      reloadTable()
    } catch (error) {
      setFormError(error.message)
      setSubmitting(false)
    }
  }

  const handlePost = async () => {
    if (!editId) return
    setSubmitting(true)
    setFormError('')
    try {
      await productionBatchesApi.post(editId)
      closeModals()
      reloadTable()
    } catch (error) {
      setFormError(error.message)
      setSubmitting(false)
    }
  }

  const handleDeleteConfirm = async () => {
    if (!deleteRow) return
    setSubmitting(true)
    setFormError('')
    try {
      await productionBatchesApi.remove(deleteRow.productionBatchId)
      closeModals()
      reloadTable()
    } catch (error) {
      setFormError(error.message)
      setSubmitting(false)
    }
  }

  const updateLine = (section, index, name, value) => {
    setForm((prev) => ({
      ...prev,
      [section]: prev[section].map((line, i) => (i === index ? { ...line, [name]: value } : line)),
    }))
  }

  const addLine = (section, emptyLine) => {
    setForm((prev) => ({ ...prev, [section]: [...prev[section], { ...emptyLine }] }))
  }

  const removeLine = (section, index) => {
    setForm((prev) => ({
      ...prev,
      [section]: prev[section].length > 1 ? prev[section].filter((_, i) => i !== index) : prev[section],
    }))
  }

  const tableOptions = useMemo(
    () => ({
      processing: true,
      serverSide: true,
      ajax: productionBatchesApi.createDataTableAjax(setLoadError),
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
        { data: 'batchNumber', name: 'batchNumber' },
        { data: 'outputWarehouseName', name: 'outputWarehouseName' },
        {
          data: 'productionDate',
          name: 'productionDate',
          render: (data) => formatJalaliDate(data),
        },
        {
          data: 'statusLabel',
          name: 'statusLabel',
          render: (data, _type, row) => {
            const badge = row.isPosted
              ? '<span class="badge badge-active">ثبت‌شده</span>'
              : '<span class="badge badge-inactive">پیش‌نویس</span>'
            if (row.isTransferredToSales) {
              return `${badge}<div class="small text-muted mt-1">منتقل به فروش</div>`
            }
            return badge
          },
        },
        { data: 'inputLinesCount', name: 'inputLinesCount', className: 'text-center' },
        { data: 'outputLinesCount', name: 'outputLinesCount', className: 'text-center' },
        { data: null, name: 'actions', defaultContent: '' },
      ],
      columnDefs: [
        { targets: 0, orderable: false, searchable: false, width: '56px', className: 'text-center' },
        { targets: [2, 4, 5, 6], orderable: false },
        {
          targets: 7,
          orderable: false,
          searchable: false,
          className: 'text-center all dt-actions-col',
          width: '160px',
        },
      ],
    }),
    [],
  )

  const actionSlots = useMemo(
    () => ({
      7: (_data, _type, row) => (
        <div className="dt-actions">
          {(canEdit || row.isPosted) && (
            <button
              type="button"
              className="dt-action-btn"
              title={row.isPosted ? 'مشاهده' : 'ویرایش'}
              onClick={() => openEdit(row, row.isPosted)}
            >
              <Icon name={row.isPosted ? 'eye' : 'edit'} />
            </button>
          )}
          {row.isPosted && (
            <button
              type="button"
              className="dt-action-btn"
              title="ردیابی"
              onClick={() => openTrace(row)}
            >
              <Icon name="route" />
            </button>
          )}
          {canDelete && !row.isPosted && (
            <button
              type="button"
              className="dt-action-btn btn-delete"
              title="حذف"
              onClick={() => setDeleteRow(row)}
            >
              <Icon name="trash" />
            </button>
          )}
        </div>
      ),
    }),
    [canDelete, canEdit, openEdit, openTrace],
  )

  return (
    <div className="content-card card border-0">
      <div className="card-body p-4">
        <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
          <div>
            <h2 className="card-title mb-1">گزارش روزانه تولید</h2>
            <p className="text-muted mb-0 small">مصرف از انبار مواد خام/نیمه‌خام و ثبت محصول تولیدی</p>
          </div>
          {canCreate && (
            <button type="button" className="btn btn-primary d-inline-flex align-items-center gap-2" onClick={openCreate}>
              <Icon name="plus" />
              <span>سند جدید</span>
            </button>
          )}
        </div>

        {loadError && <div className="alert alert-danger">{loadError}</div>}

        <DataTable ref={tableRef} options={tableOptions} actionSlots={actionSlots} />

        {showForm && (
          <div className="modal show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
            <div className="modal-dialog modal-xl modal-dialog-scrollable">
              <div className="modal-content">
                <form onSubmit={handleSubmit}>
                  <div className="modal-header">
                    <h5 className="modal-title">
                      {viewPosted ? 'مشاهده سند تولید' : editId ? 'ویرایش سند تولید' : 'سند تولید جدید'}
                    </h5>
                    <button type="button" className="btn-close" onClick={closeModals} />
                  </div>
                  <div className="modal-body">
                    {formError && <div className="alert alert-danger">{formError}</div>}
                    <div className="row g-3 mb-3">
                      <div className="col-md-3">
                        <label className="form-label">تاریخ تولید</label>
                        <JalaliDateField
                          value={form.productionDate}
                          onChange={(value) => setForm((prev) => ({ ...prev, productionDate: value }))}
                          required
                          disabled={viewPosted}
                        />
                      </div>
                      <div className="col-md-3">
                        <label className="form-label">انبار مقصد (پردازش‌شده)</label>
                        <SearchableSelect
                          options={processedWarehouses}
                          value={form.outputWarehouseId}
                          onChange={(value) => setForm((prev) => ({ ...prev, outputWarehouseId: value }))}
                          required
                          disabled={viewPosted}
                        />
                      </div>
                      <div className="col-md-3">
                        <label className="form-label">هزینه ثابت</label>
                        <AmountField
                          value={form.fixedCost}
                          onChange={(value) => setForm((prev) => ({ ...prev, fixedCost: value }))}
                          disabled={viewPosted}
                          min="0"
                        />
                      </div>
                      <div className="col-md-3">
                        <label className="form-label">هزینه متغیر</label>
                        <AmountField
                          value={form.variableCost}
                          onChange={(value) => setForm((prev) => ({ ...prev, variableCost: value }))}
                          disabled={viewPosted}
                          min="0"
                        />
                      </div>
                      <div className="col-12">
                        <label className="form-label">توضیحات</label>
                        <input
                          type="text"
                          className="form-control"
                          value={form.description}
                          disabled={viewPosted}
                          onChange={(e) => setForm((prev) => ({ ...prev, description: e.target.value }))}
                        />
                      </div>
                    </div>

                    <h6 className="mb-2">مصرف مواد (انبار خام/نیمه‌خام)</h6>
                    <div className="table-responsive mb-4">
                      <table className="table table-sm align-middle">
                        <thead>
                          <tr>
                            <th>انبار</th>
                            <th>محصول</th>
                            <th>واحد</th>
                            <th>مقدار</th>
                            {!viewPosted && <th />}
                          </tr>
                        </thead>
                        <tbody>
                          {form.inputLines.map((line, index) => (
                            <tr key={`in-${index}`}>
                              <td>
                                <SearchableSelect
                                  options={materialWarehouses}
                                  value={line.warehouseId}
                                  onChange={(value) => updateLine('inputLines', index, 'warehouseId', value)}
                                  size="sm"
                                  required
                                  disabled={viewPosted}
                                />
                              </td>
                              <td>
                                <SearchableSelect
                                  options={products}
                                  value={line.productId}
                                  onChange={(value) => updateLine('inputLines', index, 'productId', value)}
                                  size="sm"
                                  required
                                  disabled={viewPosted}
                                />
                              </td>
                              <td>
                                <select
                                  className="form-select form-select-sm"
                                  value={line.meaurmentId}
                                  required
                                  disabled={viewPosted}
                                  onChange={(e) => updateLine('inputLines', index, 'meaurmentId', e.target.value)}
                                >
                                  <option value="">—</option>
                                  {meaurmentsForProduct(line.productId).map((m) => (
                                    <option key={m.value} value={m.value}>{m.label}</option>
                                  ))}
                                </select>
                              </td>
                              <td>
                                <input
                                  type="number"
                                  className="form-control form-control-sm"
                                  value={line.quantity}
                                  min="0"
                                  step="any"
                                  required
                                  disabled={viewPosted}
                                  onChange={(e) => updateLine('inputLines', index, 'quantity', e.target.value)}
                                />
                              </td>
                              {!viewPosted && (
                                <td>
                                  <button type="button" className="btn btn-sm btn-outline-danger" onClick={() => removeLine('inputLines', index)}>
                                    <Icon name="trash" />
                                  </button>
                                </td>
                              )}
                            </tr>
                          ))}
                        </tbody>
                      </table>
                      {!viewPosted && (
                        <button type="button" className="btn btn-sm btn-outline-secondary" onClick={() => addLine('inputLines', emptyInputLine)}>
                          <Icon name="plus" /> ردیف مصرف
                        </button>
                      )}
                    </div>

                    <h6 className="mb-2">محصول تولیدی</h6>
                    <div className="table-responsive">
                      <table className="table table-sm align-middle">
                        <thead>
                          <tr>
                            <th>محصول</th>
                            <th>واحد</th>
                            <th>مقدار</th>
                            {!viewPosted && <th />}
                          </tr>
                        </thead>
                        <tbody>
                          {form.outputLines.map((line, index) => (
                            <tr key={`out-${index}`}>
                              <td>
                                <SearchableSelect
                                  options={products}
                                  value={line.productId}
                                  onChange={(value) => updateLine('outputLines', index, 'productId', value)}
                                  size="sm"
                                  required
                                  disabled={viewPosted}
                                />
                              </td>
                              <td>
                                <select
                                  className="form-select form-select-sm"
                                  value={line.meaurmentId}
                                  required
                                  disabled={viewPosted}
                                  onChange={(e) => updateLine('outputLines', index, 'meaurmentId', e.target.value)}
                                >
                                  <option value="">—</option>
                                  {meaurmentsForProduct(line.productId).map((m) => (
                                    <option key={m.value} value={m.value}>{m.label}</option>
                                  ))}
                                </select>
                              </td>
                              <td>
                                <input
                                  type="number"
                                  className="form-control form-control-sm"
                                  value={line.quantity}
                                  min="0"
                                  step="any"
                                  required
                                  disabled={viewPosted}
                                  onChange={(e) => updateLine('outputLines', index, 'quantity', e.target.value)}
                                />
                              </td>
                              {!viewPosted && (
                                <td>
                                  <button type="button" className="btn btn-sm btn-outline-danger" onClick={() => removeLine('outputLines', index)}>
                                    <Icon name="trash" />
                                  </button>
                                </td>
                              )}
                            </tr>
                          ))}
                        </tbody>
                      </table>
                      {!viewPosted && (
                        <button type="button" className="btn btn-sm btn-outline-secondary" onClick={() => addLine('outputLines', emptyOutputLine)}>
                          <Icon name="plus" /> ردیف تولید
                        </button>
                      )}
                    </div>
                  </div>
                  <div className="modal-footer">
                    <button type="button" className="btn btn-secondary" onClick={closeModals}>بستن</button>
                    {!viewPosted && editId && (
                      <button type="button" className="btn btn-success" disabled={submitting} onClick={handlePost}>
                        ثبت نهایی
                      </button>
                    )}
                    {!viewPosted && (
                      <button type="submit" className="btn btn-primary" disabled={submitting}>
                        {editId ? 'ذخیره' : 'ایجاد'}
                      </button>
                    )}
                  </div>
                </form>
              </div>
            </div>
          </div>
        )}

        {traceData && (
          <div className="modal show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
            <div className="modal-dialog modal-lg modal-dialog-scrollable">
              <div className="modal-content">
                <div className="modal-header">
                  <h5 className="modal-title">ردیابی تولید — {traceData.batchNumber}</h5>
                  <button type="button" className="btn-close" onClick={() => setTraceData(null)} />
                </div>
                <div className="modal-body">
                  <p className="small text-muted mb-3">
                    تاریخ: {formatJalaliDate(traceData.productionDate)} — انبار: {traceData.outputWarehouseName}
                  </p>
                  <h6>مصرف مواد</h6>
                  <ul className="list-group mb-3">
                    {(traceData.inputLines ?? []).map((line, i) => (
                      <li key={i} className="list-group-item d-flex justify-content-between">
                        <span>{line.productName} ({line.warehouseName}) — {formatAmount(line.quantity)} {line.meaurmentName}</span>
                        <span className="text-muted">{formatAmount(line.materialCostInBase)}</span>
                      </li>
                    ))}
                  </ul>
                  <h6>محصول تولیدی</h6>
                  <ul className="list-group mb-3">
                    {(traceData.outputLines ?? []).map((line, i) => (
                      <li key={i} className="list-group-item d-flex justify-content-between">
                        <span>{line.productName} — {formatAmount(line.quantity)} {line.meaurmentName}</span>
                        <span className="text-muted">بها: {formatAmount(line.unitCostInBase)}</span>
                      </li>
                    ))}
                  </ul>
                  {(traceData.purchaseInvoices ?? []).length > 0 && (
                    <>
                      <h6>فاکتورهای خرید (ورود به فروش)</h6>
                      <ul className="list-group">
                        {traceData.purchaseInvoices.map((inv) => (
                          <li key={inv.purchaseInvoiceId} className="list-group-item">
                            {inv.invoiceNumber} — {formatJalaliDate(inv.invoiceDate)} — {formatAmount(inv.totalAmount)}
                          </li>
                        ))}
                      </ul>
                    </>
                  )}
                </div>
              </div>
            </div>
          </div>
        )}

        {deleteRow && (
          <div className="modal show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
            <div className="modal-dialog">
              <div className="modal-content">
                <div className="modal-header">
                  <h5 className="modal-title">حذف سند تولید</h5>
                  <button type="button" className="btn-close" onClick={closeModals} />
                </div>
                <div className="modal-body">
                  {formError && <div className="alert alert-danger">{formError}</div>}
                  <p>آیا از حذف سند «{deleteRow.batchNumber}» مطمئن هستید؟</p>
                </div>
                <div className="modal-footer">
                  <button type="button" className="btn btn-secondary" onClick={closeModals}>انصراف</button>
                  <button type="button" className="btn btn-danger" disabled={submitting} onClick={handleDeleteConfirm}>حذف</button>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

export default DailyProductionPage
