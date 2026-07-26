import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import Icon from '../../components/common/Icon'
import AmountField from '../../components/common/AmountField'
import SearchableSelect from '../../components/common/SearchableSelect'
import DataTable from '../../lib/dataTableSetup'
import { usePageCrud } from '../../permissions/usePageCrud'
import { fetchProductionMaterialWarehouses } from '../../services/inventoryApi'
import { fetchMeaurmentOptions, fetchProductOptions } from '../../services/productsApi'
import {
  buildProductionFormulaPayload,
  PRODUCTION_COST_AMOUNT_MODE_OPTIONS,
  PRODUCTION_COST_TYPE_OPTIONS,
  PRODUCTION_FORMULA_MODE,
  PRODUCTION_FORMULA_MODE_OPTIONS,
  productionFormulasApi,
} from '../../services/productionApi'
import { dataTableLanguage, formatAmount } from '../Transport/CrudTablePage'

const emptyMaterialLine = { productId: '', meaurmentId: '', quantity: '', defaultWarehouseId: '' }
const emptyCostLine = { costType: '', description: '', amountMode: '1', amount: '' }

const formulaColumns = [
  { data: 'name', title: 'نام' },
  { data: 'productName', title: 'محصول' },
  { data: 'baseQuantity', title: 'مقدار پایه' },
  { data: 'modeLabel', title: 'حالت' },
  { data: 'isDefault', title: 'پیش‌فرض' },
  { data: 'materialLinesCount', title: 'مواد' },
  { data: 'costLinesCount', title: 'هزینه' },
]

function ProductionFormulasPage() {
  const tableRef = useRef(null)
  const { canCreate, canEdit, canDelete } = usePageCrud('/production/formulas')
  const [loadError, setLoadError] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editId, setEditId] = useState(null)
  const [deleteRow, setDeleteRow] = useState(null)
  const [formError, setFormError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [showAdvancedMode, setShowAdvancedMode] = useState(false)
  const [products, setProducts] = useState([])
  const [meaurments, setMeaurments] = useState([])
  const [materialWarehouses, setMaterialWarehouses] = useState([])
  const [form, setForm] = useState({
    name: '',
    productId: '',
    meaurmentId: '',
    baseQuantity: '1',
    mode: String(PRODUCTION_FORMULA_MODE.Fixed),
    isDefault: false,
    notes: '',
    materialLines: [{ ...emptyMaterialLine }],
    costLines: [],
  })

  useEffect(() => {
    fetchProductOptions().then(setProducts).catch(() => setProducts([]))
    fetchMeaurmentOptions().then(setMeaurments).catch(() => setMeaurments([]))
    fetchProductionMaterialWarehouses().then(setMaterialWarehouses).catch(() => setMaterialWarehouses([]))
  }, [])

  const meaurmentsForProduct = useCallback(
    (productId) => {
      const product = products.find((item) => String(item.value) === String(productId))
      if (!product?.baseMeaurmentId) return meaurments
      return meaurments.filter(
        (item) => item.baseMeaurmentId === product.baseMeaurmentId || item.value === product.baseMeaurmentId,
      )
    },
    [meaurments, products],
  )

  const reloadTable = useCallback(() => tableRef.current?.dt()?.ajax.reload(null, false), [])

  const closeModals = useCallback(() => {
    setShowForm(false)
    setEditId(null)
    setDeleteRow(null)
    setFormError('')
    setSubmitting(false)
    setShowAdvancedMode(false)
  }, [])

  const openCreate = useCallback(() => {
    setForm({
      name: '',
      productId: '',
      meaurmentId: '',
      baseQuantity: '1',
      mode: String(PRODUCTION_FORMULA_MODE.Fixed),
      isDefault: false,
      notes: '',
      materialLines: [{ ...emptyMaterialLine }],
      costLines: [],
    })
    setEditId(null)
    setFormError('')
    setShowAdvancedMode(false)
    setShowForm(true)
  }, [])

  const openEdit = useCallback(async (row) => {
    setFormError('')
    try {
      const formula = await productionFormulasApi.getById(row.productionFormulaId)
      const mode = String(formula.mode ?? PRODUCTION_FORMULA_MODE.Fixed)
      setForm({
        name: formula.name ?? '',
        productId: formula.productId ?? '',
        meaurmentId: formula.meaurmentId ?? '',
        baseQuantity: formula.baseQuantity ?? '1',
        mode,
        isDefault: Boolean(formula.isDefault),
        notes: formula.notes ?? '',
        materialLines: (formula.materialLines ?? []).map((line) => ({
          productId: line.productId,
          meaurmentId: line.meaurmentId,
          quantity: line.quantity,
          defaultWarehouseId: line.defaultWarehouseId ?? '',
        })),
        costLines: (formula.costLines ?? []).map((line) => ({
          costType: line.costType,
          description: line.description ?? '',
          amountMode: String(line.amountMode ?? 1),
          amount: line.amount,
        })),
      })
      setShowAdvancedMode(Number(mode) === PRODUCTION_FORMULA_MODE.Variable)
      setEditId(formula.productionFormulaId)
      setShowForm(true)
    } catch (error) {
      setLoadError(error.message)
    }
  }, [])

  const updateLine = (section, index, name, value) => {
    setForm((prev) => ({
      ...prev,
      [section]: prev[section].map((line, lineIndex) => (
        lineIndex === index ? { ...line, [name]: value } : line
      )),
    }))
  }

  const addLine = (section, emptyLine) => {
    setForm((prev) => ({ ...prev, [section]: [...prev[section], { ...emptyLine }] }))
  }

  const removeLine = (section, index) => {
    setForm((prev) => ({
      ...prev,
      [section]: prev[section].filter((_, lineIndex) => lineIndex !== index),
    }))
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    setSubmitting(true)
    setFormError('')
    try {
      const payload = buildProductionFormulaPayload({
        ...form,
        mode: showAdvancedMode ? form.mode : String(PRODUCTION_FORMULA_MODE.Fixed),
      })
      if (editId) {
        await productionFormulasApi.update(editId, payload)
      } else {
        await productionFormulasApi.create(payload)
      }
      closeModals()
      reloadTable()
    } catch (error) {
      setFormError(error.message)
      setSubmitting(false)
    }
  }

  const handleSetDefault = useCallback(async (row) => {
    setLoadError('')
    try {
      await productionFormulasApi.setDefault(row.productionFormulaId)
      reloadTable()
    } catch (error) {
      setLoadError(error.message)
    }
  }, [reloadTable])

  const handleDeleteConfirm = async () => {
    if (!deleteRow) return
    setSubmitting(true)
    setFormError('')
    try {
      await productionFormulasApi.remove(deleteRow.productionFormulaId)
      closeModals()
      reloadTable()
    } catch (error) {
      setFormError(error.message)
      setSubmitting(false)
    }
  }

  const tableOptions = useMemo(
    () => ({
      processing: true,
      serverSide: true,
      ajax: productionFormulasApi.createDataTableAjax(setLoadError),
      paging: true,
      searching: true,
      ordering: true,
      info: true,
      scrollX: true,
      autoWidth: false,
      responsive: true,
      stripeClasses: ['odd', 'even'],
      order: [[1, 'asc']],
      pageLength: 15,
      lengthMenu: [10, 15, 25, 50, 100],
      language: dataTableLanguage,
      layout: {
        topStart: { search: { placeholder: 'جستجو...' }, pageLength: { menu: [10, 15, 25, 50, 100] } },
        topEnd: null,
        bottomStart: 'info',
        bottomEnd: { paging: { firstLast: true, previousNext: true, numbers: 5 } },
      },
      columns: [
        { data: 'rowNumber', name: 'rowNumber' },
        { data: 'name', name: 'name', title: 'نام' },
        { data: 'productName', name: 'productName', title: 'محصول' },
        {
          data: 'baseQuantity',
          name: 'baseQuantity',
          title: 'مقدار پایه',
          render: (data, _type, row) => `${formatAmount(data)} ${row.meaurmentName ?? ''}`.trim(),
        },
        { data: 'modeLabel', name: 'mode', title: 'حالت' },
        {
          data: 'isDefault',
          name: 'isDefault',
          title: 'پیش‌فرض',
          render: (data) => (data
            ? '<span class="badge badge-active">پیش‌فرض</span>'
            : '<span class="text-muted">—</span>'),
        },
        { data: 'materialLinesCount', name: 'materialLinesCount', title: 'مواد', className: 'text-center' },
        { data: 'costLinesCount', name: 'costLinesCount', title: 'هزینه', className: 'text-center' },
        { data: null, name: 'actions', defaultContent: '', title: 'عملیات' },
      ],
      columnDefs: [
        { targets: 0, orderable: false, searchable: false, width: '56px', className: 'text-center' },
        { targets: [5, 6, 7], orderable: false, searchable: false },
        { targets: 8, orderable: false, searchable: false, className: 'text-center all dt-actions-col', width: '140px' },
      ],
    }),
    [],
  )

  const actionSlots = useMemo(
    () => ({
      8: (_data, _type, row) => (
        <div className="dt-actions">
          {canEdit && (
            <button type="button" className="dt-action-btn" title="ویرایش" onClick={() => openEdit(row)}>
              <Icon name="edit" />
            </button>
          )}
          {canEdit && !row.isDefault && (
            <button type="button" className="dt-action-btn" title="تنظیم به‌عنوان پیش‌فرض" onClick={() => handleSetDefault(row)}>
              <Icon name="check" />
            </button>
          )}
          {canDelete && (
            <button type="button" className="dt-action-btn btn-delete" title="حذف" onClick={() => setDeleteRow(row)}>
              <Icon name="trash" />
            </button>
          )}
        </div>
      ),
    }),
    [canDelete, canEdit, handleSetDefault, openEdit],
  )

  return (
    <div className="content-card card border-0 production-page">
      <div className="card-body p-4">
        <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
          <div>
            <h2 className="card-title mb-1">فرمول‌های ساخت</h2>
            <p className="text-muted mb-0 small">مواد اولیه و هزینه‌های استاندارد تولید را تعریف کنید</p>
          </div>
          {canCreate && (
            <button type="button" className="btn btn-primary d-inline-flex align-items-center gap-2" onClick={openCreate}>
              <Icon name="plus" />
              <span>فرمول جدید</span>
            </button>
          )}
        </div>

        {loadError && <div className="alert alert-danger">{loadError}</div>}
        <div className="users-table-wrapper">
          <DataTable ref={tableRef} className="table table-hover w-100 align-middle" options={tableOptions} slots={actionSlots}>
            <thead>
              <tr>
                <th>#</th>
                {formulaColumns.map((col) => (
                  <th key={col.data}>{col.title}</th>
                ))}
                <th>عملیات</th>
              </tr>
            </thead>
          </DataTable>
        </div>

        {showForm && (
          <div className="modal show d-block production-modal" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
            <div className="modal-dialog modal-xl modal-dialog-scrollable">
              <div className="modal-content">
                <form onSubmit={handleSubmit}>
                  <div className="modal-header">
                    <h5 className="modal-title">{editId ? 'ویرایش فرمول ساخت' : 'فرمول ساخت جدید'}</h5>
                    <button type="button" className="btn-close" onClick={closeModals} />
                  </div>
                  <div className="modal-body">
                    {formError && <div className="alert alert-danger">{formError}</div>}
                    <div className="row g-3 mb-4">
                      <div className="col-md-6">
                        <label className="form-label">نام فرمول</label>
                        <input className="form-control" value={form.name} required onChange={(event) => setForm((prev) => ({ ...prev, name: event.target.value }))} />
                      </div>
                      <div className="col-md-3">
                        <label className="form-label">محصول خروجی</label>
                        <SearchableSelect options={products} value={form.productId} required onChange={(value) => setForm((prev) => ({ ...prev, productId: value, meaurmentId: '' }))} />
                      </div>
                      <div className="col-md-3">
                        <label className="form-label">واحد</label>
                        <select className="form-select" value={form.meaurmentId} required onChange={(event) => setForm((prev) => ({ ...prev, meaurmentId: event.target.value }))}>
                          <option value="">—</option>
                          {meaurmentsForProduct(form.productId).map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}
                        </select>
                      </div>
                      <div className="col-md-3">
                        <label className="form-label">مقدار پایه</label>
                        <input type="number" min="0" step="any" className="form-control" value={form.baseQuantity} required onChange={(event) => setForm((prev) => ({ ...prev, baseQuantity: event.target.value }))} />
                        <div className="form-text">
                          فرمول برای این مقدار خروجی تعریف می‌شود؛ مصرف مواد و هزینه‌های «به ازای پایه» نسبت به آن مقیاس می‌گیرند.
                        </div>
                      </div>
                      <div className="col-md-3 d-flex align-items-end">
                        <div className="form-check mb-2">
                          <input id="formula-default" type="checkbox" className="form-check-input" checked={form.isDefault} onChange={(event) => setForm((prev) => ({ ...prev, isDefault: event.target.checked }))} />
                          <label className="form-check-label" htmlFor="formula-default">فرمول پیش‌فرض محصول</label>
                        </div>
                      </div>
                      <div className="col-md-6 d-flex align-items-end">
                        <div className="form-check mb-2">
                          <input
                            id="formula-advanced"
                            type="checkbox"
                            className="form-check-input"
                            checked={showAdvancedMode}
                            onChange={(event) => {
                              const checked = event.target.checked
                              setShowAdvancedMode(checked)
                              if (!checked) {
                                setForm((prev) => ({ ...prev, mode: String(PRODUCTION_FORMULA_MODE.Fixed) }))
                              }
                            }}
                          />
                          <label className="form-check-label" htmlFor="formula-advanced">حالت پیشرفته (فرمول متغیر)</label>
                        </div>
                      </div>
                      {showAdvancedMode && (
                        <div className="col-md-3">
                          <label className="form-label">حالت فرمول</label>
                          <select className="form-select" value={form.mode} onChange={(event) => setForm((prev) => ({ ...prev, mode: event.target.value }))}>
                            {PRODUCTION_FORMULA_MODE_OPTIONS.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}
                          </select>
                          <div className="form-text">در حالت متغیر، مواد و هزینه‌ها هنگام ثبت تولید قابل ویرایش‌اند.</div>
                        </div>
                      )}
                      <div className="col-12">
                        <label className="form-label">یادداشت</label>
                        <input className="form-control" value={form.notes} onChange={(event) => setForm((prev) => ({ ...prev, notes: event.target.value }))} />
                      </div>
                    </div>

                    <div className="d-flex justify-content-between align-items-center mb-2">
                      <h6 className="mb-0">مواد مصرفی</h6>
                      <button type="button" className="btn btn-sm btn-outline-secondary" onClick={() => addLine('materialLines', emptyMaterialLine)}>
                        <Icon name="plus" /> ردیف ماده
                      </button>
                    </div>
                    <div className="table-responsive mb-4">
                      <table className="table table-sm align-middle production-lines-table">
                        <thead><tr><th>محصول</th><th>واحد</th><th>مقدار برای پایه</th><th>انبار پیش‌فرض</th><th /></tr></thead>
                        <tbody>
                          {form.materialLines.map((line, index) => (
                            <tr key={`material-${index}`}>
                              <td><SearchableSelect options={products} value={line.productId} size="sm" required onChange={(value) => { updateLine('materialLines', index, 'productId', value); updateLine('materialLines', index, 'meaurmentId', '') }} /></td>
                              <td><select className="form-select form-select-sm" value={line.meaurmentId} required onChange={(event) => updateLine('materialLines', index, 'meaurmentId', event.target.value)}><option value="">—</option>{meaurmentsForProduct(line.productId).map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}</select></td>
                              <td><input type="number" min="0" step="any" className="form-control form-control-sm" value={line.quantity} required onChange={(event) => updateLine('materialLines', index, 'quantity', event.target.value)} /></td>
                              <td><SearchableSelect options={materialWarehouses} value={line.defaultWarehouseId} size="sm" onChange={(value) => updateLine('materialLines', index, 'defaultWarehouseId', value)} /></td>
                              <td><button type="button" className="btn btn-sm btn-outline-danger" disabled={form.materialLines.length === 1} onClick={() => removeLine('materialLines', index)}><Icon name="trash" /></button></td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>

                    <div className="d-flex justify-content-between align-items-center mb-2">
                      <h6 className="mb-0">هزینه‌های تبدیل</h6>
                      <button type="button" className="btn btn-sm btn-outline-secondary" onClick={() => addLine('costLines', emptyCostLine)}>
                        <Icon name="plus" /> ردیف هزینه
                      </button>
                    </div>
                    <div className="table-responsive">
                      <table className="table table-sm align-middle production-lines-table">
                        <thead><tr><th>نوع هزینه</th><th>شرح</th><th>روش محاسبه</th><th>مبلغ</th><th /></tr></thead>
                        <tbody>
                          {form.costLines.map((line, index) => (
                            <tr key={`cost-${index}`}>
                              <td><select className="form-select form-select-sm" value={line.costType} required onChange={(event) => updateLine('costLines', index, 'costType', event.target.value)}><option value="">—</option>{PRODUCTION_COST_TYPE_OPTIONS.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}</select></td>
                              <td><input className="form-control form-control-sm" value={line.description} onChange={(event) => updateLine('costLines', index, 'description', event.target.value)} /></td>
                              <td><select className="form-select form-select-sm" value={line.amountMode} onChange={(event) => updateLine('costLines', index, 'amountMode', event.target.value)}>{PRODUCTION_COST_AMOUNT_MODE_OPTIONS.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}</select></td>
                              <td><AmountField value={line.amount} className="amount-field-sm" min="0" onChange={(value) => updateLine('costLines', index, 'amount', value)} /></td>
                              <td><button type="button" className="btn btn-sm btn-outline-danger" onClick={() => removeLine('costLines', index)}><Icon name="trash" /></button></td>
                            </tr>
                          ))}
                          {form.costLines.length === 0 && <tr><td colSpan="5" className="text-center text-muted">هزینه‌ای تعریف نشده است.</td></tr>}
                        </tbody>
                      </table>
                    </div>
                  </div>
                  <div className="modal-footer">
                    <button type="button" className="btn btn-secondary" onClick={closeModals}>انصراف</button>
                    <button type="submit" className="btn btn-primary" disabled={submitting}>ذخیره</button>
                  </div>
                </form>
              </div>
            </div>
          </div>
        )}

        {deleteRow && (
          <div className="modal show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
            <div className="modal-dialog"><div className="modal-content">
              <div className="modal-header"><h5 className="modal-title">حذف فرمول ساخت</h5><button type="button" className="btn-close" onClick={closeModals} /></div>
              <div className="modal-body">{formError && <div className="alert alert-danger">{formError}</div>}<p>فرمول «{deleteRow.name}» حذف شود؟</p></div>
              <div className="modal-footer"><button type="button" className="btn btn-secondary" onClick={closeModals}>انصراف</button><button type="button" className="btn btn-danger" disabled={submitting} onClick={handleDeleteConfirm}>حذف</button></div>
            </div></div>
          </div>
        )}
      </div>
    </div>
  )
}

export default ProductionFormulasPage
