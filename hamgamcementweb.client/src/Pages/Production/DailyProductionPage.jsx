import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import Icon from '../../components/common/Icon'
import AmountField from '../../components/common/AmountField'
import JalaliDateField from '../../components/common/JalaliDateField'
import SearchableSelect from '../../components/common/SearchableSelect'
import DataTable from '../../lib/dataTableSetup'
import { usePageCrud } from '../../permissions/usePageCrud'
import { todayGregorianIso } from '../../lib/afghanSolarCalendar'
import { fetchProcessedWarehouseOptions, fetchProductionMaterialWarehouses } from '../../services/inventoryApi'
import { fetchMeaurmentOptions, fetchProductOptions } from '../../services/productsApi'
import {
  buildProductionBatchPayload,
  PRODUCTION_COST_TYPE_OPTIONS,
  PRODUCTION_FORMULA_MODE,
  productionBatchesApi,
  productionFormulasApi,
  productionPlansApi,
  scaleFormulaForProduction,
} from '../../services/productionApi'
import { dataTableLanguage, formatAmount, formatJalaliDate } from '../Transport/CrudTablePage'

const emptyInputLine = { warehouseId: '', productId: '', meaurmentId: '', quantity: '' }
const emptyCostLine = { costType: '', description: '', amount: '' }
const emptyForm = () => ({
  productionDate: todayGregorianIso(),
  productionFormulaId: '',
  productionPlanId: '',
  producedQuantity: '',
  outputWarehouseId: '',
  outputProductName: '',
  outputMeaurmentName: '',
  formulaMode: null,
  description: '',
  inputLines: [],
  costLines: [],
})

const batchColumns = [
  { data: 'batchNumber', title: 'شماره سند' },
  { data: 'formulaName', title: 'فرمول' },
  { data: 'productionDate', title: 'تاریخ' },
  { data: 'outputWarehouseName', title: 'انبار مقصد' },
  { data: 'statusLabel', title: 'وضعیت' },
  { data: 'totalCostInBase', title: 'بهای تمام‌شده' },
]

function costTypeLabel(value) {
  return PRODUCTION_COST_TYPE_OPTIONS.find((item) => item.value === Number(value))?.label ?? value
}

function DailyProductionPage() {
  const tableRef = useRef(null)
  const [searchParams, setSearchParams] = useSearchParams()
  const { canCreate, canEdit, canDelete } = usePageCrud('/production/daily')
  const [loadError, setLoadError] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editId, setEditId] = useState(null)
  const [viewPosted, setViewPosted] = useState(false)
  const [deleteRow, setDeleteRow] = useState(null)
  const [traceData, setTraceData] = useState(null)
  const [postPreview, setPostPreview] = useState(null)
  const [postTargetId, setPostTargetId] = useState(null)
  const [formError, setFormError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [formulaOptions, setFormulaOptions] = useState([])
  const [planOptions, setPlanOptions] = useState([])
  const [materialWarehouses, setMaterialWarehouses] = useState([])
  const [processedWarehouses, setProcessedWarehouses] = useState([])
  const [products, setProducts] = useState([])
  const [meaurments, setMeaurments] = useState([])
  const [formula, setFormula] = useState(null)
  const [form, setForm] = useState(emptyForm)
  const planBootstrapDone = useRef(false)

  useEffect(() => {
    productionFormulasApi.fetchOptions().then(setFormulaOptions).catch(() => setFormulaOptions([]))
    productionPlansApi.fetchOptions().then(setPlanOptions).catch(() => setPlanOptions([]))
    fetchProductionMaterialWarehouses().then(setMaterialWarehouses).catch(() => setMaterialWarehouses([]))
    fetchProcessedWarehouseOptions().then(setProcessedWarehouses).catch(() => setProcessedWarehouses([]))
    fetchProductOptions().then(setProducts).catch(() => setProducts([]))
    fetchMeaurmentOptions().then(setMeaurments).catch(() => setMeaurments([]))
  }, [])

  const meaurmentsForProduct = useCallback((productId) => {
    const product = products.find((item) => String(item.value) === String(productId))
    if (!product?.baseMeaurmentId) return meaurments
    return meaurments.filter((item) => item.baseMeaurmentId === product.baseMeaurmentId || item.value === product.baseMeaurmentId)
  }, [meaurments, products])

  const reloadTable = useCallback(() => tableRef.current?.dt()?.ajax.reload(null, false), [])
  const isFixedFormula = Number(form.formulaMode) === PRODUCTION_FORMULA_MODE.Fixed

  const conversionCostPreview = useMemo(
    () => (form.costLines || []).reduce((sum, line) => sum + (Number(line.amount) || 0), 0),
    [form.costLines],
  )

  const closeModals = useCallback(() => {
    setShowForm(false)
    setEditId(null)
    setViewPosted(false)
    setDeleteRow(null)
    setTraceData(null)
    setPostPreview(null)
    setPostTargetId(null)
    setFormError('')
    setSubmitting(false)
    setFormula(null)
  }, [])

  const applyFormulaScale = useCallback((nextFormula, quantity) => {
    const scaled = scaleFormulaForProduction(nextFormula, quantity)
    setForm((prev) => ({
      ...prev,
      inputLines: scaled.inputLines,
      costLines: scaled.costLines,
      outputProductName: scaled.outputProductName ?? '',
      outputMeaurmentName: scaled.outputMeaurmentName ?? '',
      formulaMode: scaled.mode,
    }))
  }, [])

  const handleFormulaChange = async (productionFormulaId) => {
    setFormError('')
    setFormula(null)
    setForm((prev) => ({
      ...prev,
      productionFormulaId,
      inputLines: [],
      costLines: [],
      outputProductName: '',
      outputMeaurmentName: '',
      formulaMode: null,
    }))
    if (!productionFormulaId) return
    try {
      const selected = await productionFormulasApi.getForProduction(productionFormulaId)
      setFormula(selected)
      const matchingPlans = await productionPlansApi.fetchOptions(selected.productId).catch(() => [])
      setPlanOptions((prev) => {
        const others = prev.filter((item) => String(item.productId) !== String(selected.productId))
        return [...matchingPlans, ...others]
      })
      applyFormulaScale(selected, form.producedQuantity)
    } catch (error) {
      setFormError(error.message)
    }
  }

  const handleProducedQuantityChange = (producedQuantity) => {
    setForm((prev) => ({ ...prev, producedQuantity }))
    if (formula) applyFormulaScale(formula, producedQuantity)
  }

  const handlePlanChange = async (productionPlanId) => {
    setForm((prev) => ({ ...prev, productionPlanId }))
    if (!productionPlanId) return
    try {
      const plan = await productionPlansApi.getById(productionPlanId)
      setForm((prev) => ({
        ...prev,
        productionPlanId,
        producedQuantity: plan.plannedQuantity ?? prev.producedQuantity,
        productionDate: plan.planDate || prev.productionDate,
      }))
      const formulaId = plan.defaultFormulaId || form.productionFormulaId
      if (formulaId) {
        const selected = await productionFormulasApi.getForProduction(formulaId)
        setFormula(selected)
        setForm((prev) => ({ ...prev, productionFormulaId: formulaId }))
        applyFormulaScale(selected, plan.plannedQuantity)
      }
    } catch (error) {
      setFormError(error.message)
    }
  }

  const openCreate = useCallback(() => {
    setFormError('')
    setForm(emptyForm())
    setFormula(null)
    setEditId(null)
    setViewPosted(false)
    setShowForm(true)
  }, [])

  const openEdit = useCallback(async (row, readOnly = false) => {
    setFormError('')
    try {
      const [batch, selected] = await Promise.all([
        productionBatchesApi.getById(row.productionBatchId),
        productionFormulasApi.getForProduction(row.productionFormulaId),
      ])
      const output = batch.outputLines?.[0]
      setFormula(selected)
      setForm({
        productionDate: String(batch.productionDate).slice(0, 10),
        productionFormulaId: batch.productionFormulaId,
        productionPlanId: batch.productionPlanId ?? '',
        producedQuantity: output?.quantity ?? '',
        outputWarehouseId: batch.outputWarehouseId,
        outputProductName: output?.productName ?? selected.productName ?? '',
        outputMeaurmentName: output?.meaurmentName ?? selected.meaurmentName ?? '',
        formulaMode: batch.formulaMode ?? selected.mode,
        description: batch.description ?? '',
        inputLines: (batch.inputLines ?? []).map((line) => ({
          warehouseId: line.warehouseId,
          productId: line.productId,
          meaurmentId: line.meaurmentId,
          quantity: line.quantity,
          productName: line.productName,
          meaurmentName: line.meaurmentName,
        })),
        costLines: (batch.costLines ?? []).map((line) => ({
          costType: line.costType,
          description: line.description ?? '',
          amount: line.amount,
          accountId: line.accountId ?? '',
        })),
      })
      setEditId(batch.productionBatchId)
      setViewPosted(readOnly || batch.isPosted)
      setShowForm(true)
    } catch (error) {
      setLoadError(error.message)
    }
  }, [])

  useEffect(() => {
    const planId = searchParams.get('planId')
    if (!planId || planBootstrapDone.current || !canCreate) return
    planBootstrapDone.current = true

    const bootstrapFromPlan = async () => {
      setFormError('')
      setForm(emptyForm())
      setFormula(null)
      setEditId(null)
      setViewPosted(false)
      setShowForm(true)
      try {
        const plan = await productionPlansApi.getById(planId)
        setForm((prev) => ({
          ...prev,
          productionPlanId: planId,
          producedQuantity: plan.plannedQuantity ?? '',
          productionDate: plan.planDate || prev.productionDate,
        }))
        if (plan.defaultFormulaId) {
          const selected = await productionFormulasApi.getForProduction(plan.defaultFormulaId)
          setFormula(selected)
          setForm((prev) => ({
            ...prev,
            productionFormulaId: plan.defaultFormulaId,
            productionPlanId: planId,
            producedQuantity: plan.plannedQuantity ?? '',
            productionDate: plan.planDate || prev.productionDate,
          }))
          applyFormulaScale(selected, plan.plannedQuantity)
        }
      } catch (error) {
        setFormError(error.message)
      } finally {
        setSearchParams({}, { replace: true })
      }
    }

    bootstrapFromPlan()
  }, [searchParams, canCreate, applyFormulaScale, setSearchParams])

  const updateLine = (section, index, name, value) => {
    setForm((prev) => ({
      ...prev,
      [section]: prev[section].map((line, lineIndex) => (lineIndex === index ? { ...line, [name]: value } : line)),
    }))
  }

  const openPostPreview = useCallback(async (id) => {
    if (!id) return
    setSubmitting(true)
    setFormError('')
    setLoadError('')
    try {
      const preview = await productionBatchesApi.previewPost(id)
      setPostTargetId(id)
      setPostPreview(preview)
    } catch (error) {
      setLoadError(error.message)
      setFormError(error.message)
    } finally {
      setSubmitting(false)
    }
  }, [])

  const confirmPost = useCallback(async () => {
    if (!postTargetId) return
    setSubmitting(true)
    setFormError('')
    try {
      await productionBatchesApi.post(postTargetId)
      closeModals()
      reloadTable()
    } catch (error) {
      setFormError(error.message)
      setSubmitting(false)
    }
  }, [closeModals, postTargetId, reloadTable])

  const handleUnpost = useCallback(async (row) => {
    setLoadError('')
    try {
      await productionBatchesApi.unpost(row.productionBatchId)
      reloadTable()
    } catch (error) {
      setLoadError(error.message)
    }
  }, [reloadTable])

  const handleSubmit = async (event) => {
    event.preventDefault()
    if (viewPosted) return
    setSubmitting(true)
    setFormError('')
    try {
      const payload = buildProductionBatchPayload(form)
      if (editId) await productionBatchesApi.update(editId, payload)
      else await productionBatchesApi.create(payload)
      closeModals()
      reloadTable()
    } catch (error) {
      setFormError(error.message)
      setSubmitting(false)
    }
  }

  const openTrace = useCallback(async (row) => {
    try {
      setTraceData(await productionBatchesApi.trace(row.productionBatchId))
    } catch (error) {
      setLoadError(error.message)
    }
  }, [])

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

  const tableOptions = useMemo(() => ({
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
      topStart: { search: { placeholder: 'جستجو...' }, pageLength: { menu: [10, 15, 25, 50, 100] } },
      topEnd: null,
      bottomStart: 'info',
      bottomEnd: { paging: { firstLast: true, previousNext: true, numbers: 5 } },
    },
    columns: [
      { data: 'rowNumber', name: 'rowNumber' },
      { data: 'batchNumber', name: 'batchNumber', title: 'شماره سند' },
      { data: 'formulaName', name: 'formulaName', title: 'فرمول' },
      {
        data: 'productionDate',
        name: 'productionDate',
        title: 'تاریخ',
        render: (data) => formatJalaliDate(data),
      },
      { data: 'outputWarehouseName', name: 'outputWarehouseName', title: 'انبار مقصد' },
      {
        data: 'statusLabel',
        name: 'status',
        title: 'وضعیت',
        render: (_data, _type, row) =>
          row.isPosted
            ? '<span class="badge badge-active">ثبت‌شده</span>'
            : '<span class="badge badge-inactive">پیش‌نویس</span>',
      },
      {
        data: 'totalCostInBase',
        name: 'totalCostInBase',
        title: 'بهای تمام‌شده',
        render: (data) => formatAmount(data),
      },
      { data: null, name: 'actions', defaultContent: '', title: 'عملیات' },
    ],
    columnDefs: [
      { targets: 0, orderable: false, searchable: false, width: '56px', className: 'text-center' },
      { targets: [4, 5, 6], orderable: false },
      { targets: 7, orderable: false, searchable: false, className: 'text-center all dt-actions-col', width: '180px' },
    ],
  }), [])

  const actionSlots = useMemo(() => ({
    7: (_data, _type, row) => (
      <div className="dt-actions">
        {canEdit && !row.isPosted && (
          <button type="button" className="dt-action-btn" title="ویرایش" onClick={() => openEdit(row)}>
            <Icon name="edit" />
          </button>
        )}
        {canEdit && !row.isPosted && (
          <button type="button" className="dt-action-btn" title="ثبت نهایی" onClick={() => openPostPreview(row.productionBatchId)}>
            <Icon name="check" />
          </button>
        )}
        {canEdit && row.isPosted && (
          <button type="button" className="dt-action-btn" title="برگشت از ثبت نهایی" onClick={() => handleUnpost(row)}>
            <Icon name="rotate-left" />
          </button>
        )}
        {row.isPosted && (
          <button type="button" className="dt-action-btn" title="ردیابی" onClick={() => openTrace(row)}>
            <Icon name="route" />
          </button>
        )}
        {canDelete && !row.isPosted && (
          <button type="button" className="dt-action-btn btn-delete" title="حذف" onClick={() => setDeleteRow(row)}>
            <Icon name="trash" />
          </button>
        )}
      </div>
    ),
  }), [canDelete, canEdit, handleUnpost, openEdit, openPostPreview, openTrace])

  return (
    <div className="content-card card border-0 production-page">
      <div className="card-body p-4">
        <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
          <div>
            <h2 className="card-title mb-1">تولید روزانه</h2>
            <p className="text-muted mb-0 small">پیش‌نویس → بررسی هزینه → ثبت نهایی (اثر روی موجودی و دفتر)</p>
          </div>
          {canCreate && (
            <button type="button" className="btn btn-primary d-inline-flex align-items-center gap-2" onClick={openCreate}>
              <Icon name="plus" />
              <span>سند جدید</span>
            </button>
          )}
        </div>
        {loadError && <div className="alert alert-danger">{loadError}</div>}
        <div className="users-table-wrapper">
          <DataTable ref={tableRef} className="table table-hover w-100 align-middle" options={tableOptions} slots={actionSlots}>
            <thead>
              <tr>
                <th>#</th>
                {batchColumns.map((col) => (
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
                    <h5 className="modal-title">
                      {viewPosted ? 'مشاهده سند تولید' : editId ? 'ویرایش سند تولید' : 'سند تولید جدید'}
                    </h5>
                    <button type="button" className="btn-close" onClick={closeModals} />
                  </div>
                  <div className="modal-body">
                    {formError && <div className="alert alert-danger">{formError}</div>}

                    <h6 className="mb-3">اطلاعات سند</h6>
                    <div className="row g-3 mb-4">
                      <div className="col-md-3">
                        <label className="form-label">تاریخ تولید</label>
                        <JalaliDateField
                          value={form.productionDate}
                          required
                          disabled={viewPosted}
                          onChange={(value) => setForm((prev) => ({ ...prev, productionDate: value }))}
                        />
                      </div>
                      <div className="col-md-4">
                        <label className="form-label">برنامه تولید (اختیاری)</label>
                        <SearchableSelect
                          options={planOptions}
                          value={form.productionPlanId}
                          disabled={viewPosted}
                          onChange={handlePlanChange}
                        />
                      </div>
                      <div className="col-md-5">
                        <label className="form-label">فرمول ساخت</label>
                        <SearchableSelect
                          options={formulaOptions}
                          value={form.productionFormulaId}
                          required
                          disabled={viewPosted}
                          onChange={handleFormulaChange}
                        />
                      </div>
                      <div className="col-md-2">
                        <label className="form-label">مقدار تولید</label>
                        <input
                          type="number"
                          min="0"
                          step="any"
                          className="form-control"
                          value={form.producedQuantity}
                          required
                          disabled={viewPosted}
                          onChange={(event) => handleProducedQuantityChange(event.target.value)}
                        />
                      </div>
                      <div className="col-md-4">
                        <label className="form-label">محصول خروجی</label>
                        <input
                          className="form-control"
                          readOnly
                          value={form.outputProductName ? `${form.outputProductName}${form.outputMeaurmentName ? ` (${form.outputMeaurmentName})` : ''}` : ''}
                        />
                      </div>
                      <div className="col-md-3">
                        <label className="form-label">انبار مقصد (فرآوری‌شده)</label>
                        <SearchableSelect
                          options={processedWarehouses}
                          value={form.outputWarehouseId}
                          required
                          disabled={viewPosted}
                          onChange={(value) => setForm((prev) => ({ ...prev, outputWarehouseId: value }))}
                        />
                      </div>
                      <div className="col-md-3">
                        <label className="form-label">توضیحات</label>
                        <input
                          className="form-control"
                          value={form.description}
                          disabled={viewPosted}
                          onChange={(event) => setForm((prev) => ({ ...prev, description: event.target.value }))}
                        />
                      </div>
                    </div>

                    <div className="d-flex justify-content-between align-items-center mb-2">
                      <h6 className="mb-0">مصرف مواد</h6>
                      {!viewPosted && !isFixedFormula && (
                        <button
                          type="button"
                          className="btn btn-sm btn-outline-secondary"
                          onClick={() => setForm((prev) => ({ ...prev, inputLines: [...prev.inputLines, { ...emptyInputLine }] }))}
                        >
                          <Icon name="plus" /> ردیف مصرف
                        </button>
                      )}
                    </div>
                    <div className="table-responsive mb-4">
                      <table className="table table-sm align-middle production-lines-table">
                        <thead>
                          <tr>
                            <th>انبار</th>
                            <th>محصول</th>
                            <th>واحد</th>
                            <th>مقدار</th>
                            {!viewPosted && !isFixedFormula && <th />}
                          </tr>
                        </thead>
                        <tbody>
                          {form.inputLines.map((line, index) => (
                            <tr key={`input-${index}`}>
                              <td>
                                <SearchableSelect
                                  options={materialWarehouses}
                                  value={line.warehouseId}
                                  size="sm"
                                  required
                                  disabled={viewPosted}
                                  onChange={(value) => updateLine('inputLines', index, 'warehouseId', value)}
                                />
                              </td>
                              <td>
                                {isFixedFormula ? (
                                  <input
                                    className="form-control form-control-sm"
                                    readOnly
                                    value={line.productName ?? products.find((item) => String(item.value) === String(line.productId))?.label ?? ''}
                                  />
                                ) : (
                                  <SearchableSelect
                                    options={products}
                                    value={line.productId}
                                    size="sm"
                                    required
                                    disabled={viewPosted}
                                    onChange={(value) => updateLine('inputLines', index, 'productId', value)}
                                  />
                                )}
                              </td>
                              <td>
                                {isFixedFormula ? (
                                  <input
                                    className="form-control form-control-sm"
                                    readOnly
                                    value={line.meaurmentName ?? meaurments.find((item) => String(item.value) === String(line.meaurmentId))?.label ?? ''}
                                  />
                                ) : (
                                  <select
                                    className="form-select form-select-sm"
                                    value={line.meaurmentId}
                                    required
                                    disabled={viewPosted}
                                    onChange={(event) => updateLine('inputLines', index, 'meaurmentId', event.target.value)}
                                  >
                                    <option value="">—</option>
                                    {meaurmentsForProduct(line.productId).map((item) => (
                                      <option key={item.value} value={item.value}>{item.label}</option>
                                    ))}
                                  </select>
                                )}
                              </td>
                              <td>
                                <input
                                  type="number"
                                  min="0"
                                  step="any"
                                  className="form-control form-control-sm"
                                  value={line.quantity}
                                  required
                                  readOnly={isFixedFormula}
                                  disabled={viewPosted}
                                  onChange={(event) => updateLine('inputLines', index, 'quantity', event.target.value)}
                                />
                              </td>
                              {!viewPosted && !isFixedFormula && (
                                <td>
                                  <button
                                    type="button"
                                    className="btn btn-sm btn-outline-danger"
                                    disabled={form.inputLines.length === 1}
                                    onClick={() => setForm((prev) => ({
                                      ...prev,
                                      inputLines: prev.inputLines.filter((_, itemIndex) => itemIndex !== index),
                                    }))}
                                  >
                                    <Icon name="trash" />
                                  </button>
                                </td>
                              )}
                            </tr>
                          ))}
                          {form.inputLines.length === 0 && (
                            <tr><td colSpan="5" className="text-center text-muted">فرمول و مقدار تولید را انتخاب کنید.</td></tr>
                          )}
                        </tbody>
                      </table>
                    </div>

                    <div className="d-flex justify-content-between align-items-center mb-2">
                      <h6 className="mb-0">هزینه‌های تبدیل</h6>
                      {!viewPosted && !isFixedFormula && (
                        <button
                          type="button"
                          className="btn btn-sm btn-outline-secondary"
                          onClick={() => setForm((prev) => ({ ...prev, costLines: [...prev.costLines, { ...emptyCostLine }] }))}
                        >
                          <Icon name="plus" /> ردیف هزینه
                        </button>
                      )}
                    </div>
                    <div className="table-responsive mb-4">
                      <table className="table table-sm align-middle production-lines-table">
                        <thead>
                          <tr>
                            <th>نوع</th>
                            <th>شرح</th>
                            <th>مبلغ</th>
                            {!viewPosted && !isFixedFormula && <th />}
                          </tr>
                        </thead>
                        <tbody>
                          {form.costLines.map((line, index) => (
                            <tr key={`cost-${index}`}>
                              <td>
                                <input className="form-control form-control-sm" readOnly value={costTypeLabel(line.costType)} />
                              </td>
                              <td>
                                {isFixedFormula ? (
                                  <input className="form-control form-control-sm" readOnly value={line.description} />
                                ) : (
                                  <input
                                    className="form-control form-control-sm"
                                    value={line.description}
                                    disabled={viewPosted}
                                    onChange={(event) => updateLine('costLines', index, 'description', event.target.value)}
                                  />
                                )}
                              </td>
                              <td>
                                <AmountField
                                  value={line.amount}
                                  min="0"
                                  className="amount-field-sm"
                                  readOnly={isFixedFormula}
                                  disabled={viewPosted}
                                  onChange={(value) => updateLine('costLines', index, 'amount', value)}
                                />
                              </td>
                              {!viewPosted && !isFixedFormula && (
                                <td>
                                  <button
                                    type="button"
                                    className="btn btn-sm btn-outline-danger"
                                    onClick={() => setForm((prev) => ({
                                      ...prev,
                                      costLines: prev.costLines.filter((_, itemIndex) => itemIndex !== index),
                                    }))}
                                  >
                                    <Icon name="trash" />
                                  </button>
                                </td>
                              )}
                            </tr>
                          ))}
                          {form.costLines.length === 0 && (
                            <tr><td colSpan="4" className="text-center text-muted">هزینه‌ای تعریف نشده است.</td></tr>
                          )}
                        </tbody>
                      </table>
                    </div>

                    <div className="production-summary">
                      <div className="production-summary-row">
                        <span>هزینه تبدیل (از خطوط هزینه)</span>
                        <span>{formatAmount(conversionCostPreview)}</span>
                      </div>
                      <div className="production-summary-row text-muted small">
                        <span>بهای مواد</span>
                        <span>پس از ثبت نهایی از FIFO محاسبه می‌شود</span>
                      </div>
                      <div className="production-summary-row total">
                        <span>خروجی</span>
                        <span>
                          {formatAmount(form.producedQuantity)} {form.outputMeaurmentName || ''}
                          {form.outputProductName ? ` — ${form.outputProductName}` : ''}
                        </span>
                      </div>
                    </div>
                  </div>
                  <div className="modal-footer">
                    <button type="button" className="btn btn-secondary" onClick={closeModals}>بستن</button>
                    {!viewPosted && editId && (
                      <button type="button" className="btn btn-success" disabled={submitting} onClick={() => openPostPreview(editId)}>
                        ثبت نهایی
                      </button>
                    )}
                    {!viewPosted && (
                      <button type="submit" className="btn btn-primary" disabled={submitting}>
                        {editId ? 'ذخیره پیش‌نویس' : 'ایجاد پیش‌نویس'}
                      </button>
                    )}
                  </div>
                </form>
              </div>
            </div>
          </div>
        )}

        {postPreview && (
          <div className="modal show d-block production-modal" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
            <div className="modal-dialog modal-lg modal-dialog-scrollable">
              <div className="modal-content">
                <div className="modal-header">
                  <h5 className="modal-title">تأیید ثبت نهایی — {postPreview.batchNumber}</h5>
                  <button type="button" className="btn-close" onClick={() => { setPostPreview(null); setPostTargetId(null) }} />
                </div>
                <div className="modal-body">
                  {formError && <div className="alert alert-danger">{formError}</div>}
                  {(postPreview.warnings ?? []).length > 0 && (
                    <div className="alert alert-warning">
                      <ul className="mb-0 pe-3">
                        {postPreview.warnings.map((warning, index) => (
                          <li key={index}>{warning}</li>
                        ))}
                      </ul>
                    </div>
                  )}
                  <p className="small text-muted">
                    با تأیید، موجودی مواد خام/نیمه کم و محصول در انبار «{postPreview.outputWarehouseName}» زیاد می‌شود؛ سند دابل‌انتری نیز ثبت می‌گردد.
                  </p>

                  <h6>مصرف مواد (برآورد FIFO)</h6>
                  <ul className="list-group mb-3">
                    {(postPreview.inputLines ?? []).map((line, index) => (
                      <li key={index} className={`list-group-item d-flex justify-content-between ${line.hasEnoughStock ? '' : 'list-group-item-danger'}`}>
                        <span>
                          {line.productName} ({line.warehouseName}) — {formatAmount(line.quantity)} {line.meaurmentName}
                          <span className="d-block small text-muted">موجود: {formatAmount(line.availableQuantityInBase)}</span>
                        </span>
                        <span>{formatAmount(line.estimatedMaterialCostInBase)}</span>
                      </li>
                    ))}
                  </ul>

                  <h6>هزینه‌های تبدیل</h6>
                  <ul className="list-group mb-3">
                    {(postPreview.costLines ?? []).length === 0 && (
                      <li className="list-group-item text-muted">بدون هزینه تبدیل</li>
                    )}
                    {(postPreview.costLines ?? []).map((line, index) => (
                      <li key={index} className="list-group-item d-flex justify-content-between">
                        <span>{costTypeLabel(line.costType)}{line.description ? ` — ${line.description}` : ''}</span>
                        <span>{formatAmount(line.amount)}</span>
                      </li>
                    ))}
                  </ul>

                  <h6>خروجی</h6>
                  <ul className="list-group mb-3">
                    {(postPreview.outputLines ?? []).map((line, index) => (
                      <li key={index} className="list-group-item">
                        {line.productName} — {formatAmount(line.quantity)} {line.meaurmentName}
                        → انبار {postPreview.outputWarehouseName}
                      </li>
                    ))}
                  </ul>

                  <div className="production-summary">
                    <div className="production-summary-row">
                      <span>بهای مواد (برآورد)</span>
                      <span>{formatAmount(postPreview.estimatedMaterialCostInBase)}</span>
                    </div>
                    <div className="production-summary-row">
                      <span>هزینه تبدیل</span>
                      <span>{formatAmount(postPreview.conversionCostInBase)}</span>
                    </div>
                    <div className="production-summary-row total">
                      <span>بهای تمام‌شده برآوردی</span>
                      <span>{formatAmount(postPreview.estimatedTotalCostInBase)}</span>
                    </div>
                  </div>
                </div>
                <div className="modal-footer">
                  <button type="button" className="btn btn-secondary" onClick={() => { setPostPreview(null); setPostTargetId(null) }}>
                    انصراف
                  </button>
                  <button
                    type="button"
                    className="btn btn-success"
                    disabled={submitting || !postPreview.canPost}
                    onClick={confirmPost}
                  >
                    تأیید و ثبت نهایی
                  </button>
                </div>
              </div>
            </div>
          </div>
        )}

        {traceData && (
          <div className="modal show d-block production-modal" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
            <div className="modal-dialog modal-lg modal-dialog-scrollable">
              <div className="modal-content">
                <div className="modal-header">
                  <h5 className="modal-title">ردیابی تولید — {traceData.batchNumber}</h5>
                  <button type="button" className="btn-close" onClick={() => setTraceData(null)} />
                </div>
                <div className="modal-body">
                  <p className="small text-muted">
                    تاریخ: {formatJalaliDate(traceData.productionDate)} — انبار: {traceData.outputWarehouseName} — بهای تمام‌شده:{' '}
                    {formatAmount(traceData.totalCostInBase)}
                  </p>
                  <h6>مصرف مواد</h6>
                  <ul className="list-group mb-3">
                    {(traceData.inputLines ?? []).map((line, index) => (
                      <li key={index} className="list-group-item d-flex justify-content-between">
                        <span>{line.productName} ({line.warehouseName}) — {formatAmount(line.quantity)} {line.meaurmentName}</span>
                        <span>{formatAmount(line.materialCostInBase)}</span>
                      </li>
                    ))}
                  </ul>
                  <h6>هزینه‌ها</h6>
                  <ul className="list-group mb-3">
                    {(traceData.costLines ?? []).map((line, index) => (
                      <li key={index} className="list-group-item d-flex justify-content-between">
                        <span>{line.description || costTypeLabel(line.costType)}</span>
                        <span>{formatAmount(line.amount)}</span>
                      </li>
                    ))}
                  </ul>
                  <h6>محصول و Lotهای خروجی</h6>
                  <ul className="list-group mb-3">
                    {(traceData.inventoryLots ?? []).map((lot) => (
                      <li key={lot.inventoryLotId} className="list-group-item">
                        <strong>{lot.lotCode}</strong> — {lot.productName} — تولید: {formatAmount(lot.receivedQuantityInBase)}، باقی‌مانده:{' '}
                        {formatAmount(lot.remainingQuantityInBase)}
                      </li>
                    ))}
                  </ul>
                  <h6>فروش‌های مرتبط</h6>
                  <ul className="list-group mb-3">
                    {(traceData.sales ?? []).map((sale, index) => (
                      <li key={`${sale.saleInvoiceId}-${index}`} className="list-group-item">
                        {sale.invoiceNumber} — {formatJalaliDate(sale.invoiceDate)} — Lot: {sale.lotCode} — {formatAmount(sale.quantityInBase)}
                      </li>
                    ))}
                    {!(traceData.sales ?? []).length && <li className="list-group-item text-muted">فروشی ثبت نشده است.</li>}
                  </ul>
                  <h6>Lotهای مصرف‌شده</h6>
                  <ul className="list-group">
                    {(traceData.consumedLots ?? []).map((lot, index) => (
                      <li key={`${lot.inventoryLotId}-${index}`} className="list-group-item">
                        {lot.productName} — {lot.lotCode} — مقدار: {formatAmount(lot.quantityInBase)} — بها: {formatAmount(lot.lineCostInBase)}
                      </li>
                    ))}
                    {!(traceData.consumedLots ?? []).length && <li className="list-group-item text-muted">Lot مصرفی موجود نیست.</li>}
                  </ul>
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
                  <p>سند «{deleteRow.batchNumber}» حذف شود؟</p>
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
