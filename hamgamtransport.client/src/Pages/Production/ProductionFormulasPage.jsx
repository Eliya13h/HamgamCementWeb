import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import Icon from '../../components/common/Icon'
import AmountField from '../../components/common/AmountField'
import SearchableSelect from '../../components/common/SearchableSelect'
import DataTable from '../../lib/dataTableSetup'
import { tipProps, useBootstrapTooltips } from '../../hooks/useBootstrapTooltips'
import {
  useModalKeyboardShortcuts,
  usePageCreateShortcut,
  useModalAutoFocus,
} from '../../hooks/useModalKeyboardShortcuts'
import { showAppToast } from '../../lib/appToast'
import { persianValidity, validateFormPersian } from '../../lib/persianFormValidity'
import { usePageCrud } from '../../permissions/usePageCrud'
import { fetchProductionMaterialWarehouses } from '../../services/inventoryApi'
import { fetchMeaurmentOptions, fetchProductOptions, PRODUCT_KIND } from '../../services/productsApi'
import {
  buildProductionFormulaPayload,
  fetchProductionCostCategoryOptions,
  PRODUCTION_COST_AMOUNT_MODE,
  PRODUCTION_COST_AMOUNT_MODE_OPTIONS,
  PRODUCTION_COST_TYPE,
  PRODUCTION_COST_TYPE_OPTIONS,
  PRODUCTION_FORMULA_MODE,
  PRODUCTION_FORMULA_MODE_OPTIONS,
  productionFormulasApi,
} from '../../services/productionApi'
import { dataTableLanguage, formatAmount } from '../../components/common/CrudTablePage'

const SHOW_ALL_VALUE = '__show_all__'
const SHOW_ALL_OPTION = { value: SHOW_ALL_VALUE, label: '── نمایش همه محصولات ──' }

const emptyMaterialLine = { productId: '', meaurmentId: '', quantity: '', defaultWarehouseId: '' }

function emptyDynamicCostLine(categories = []) {
  const first = categories.find((c) => !c.isSystem)
  return {
    productionCostCategoryId: first ? String(first.value) : '',
    costType: String(first?.costType ?? PRODUCTION_COST_TYPE.Ancillary),
    description: first?.label ?? '',
    amountMode: String(PRODUCTION_COST_AMOUNT_MODE.PerBase),
    amount: '',
    isSystem: false,
  }
}

const formulaColumns = [
  { data: 'name', title: 'نام' },
  { data: 'productName', title: 'محصول' },
  { data: 'baseQuantity', title: 'مقدار پایه' },
  { data: 'modeLabel', title: 'حالت' },
  { data: 'isDefault', title: 'پیش‌فرض' },
  { data: 'materialLinesCount', title: 'مواد' },
  { data: 'costLinesCount', title: 'هزینه' },
]

function costCategoryLabel(categories, line) {
  const fromCat = categories.find(
    (c) => String(c.value) === String(line.productionCostCategoryId),
  )
  if (fromCat) return fromCat.label
  return PRODUCTION_COST_TYPE_OPTIONS.find((item) => item.value === Number(line.costType))?.label
    ?? line.description
    ?? line.costType
}

function resolveDefaultUnit(product) {
  if (!product) return ''
  return String(product.defaultMeaurmentId || product.baseMeaurmentId || '')
}

function resolveDefaultWarehouse(product, warehouses) {
  if (!product || !warehouses?.length) return ''
  const kind = Number(product.productKind)
  const matched = warehouses.find((item) => Number(item.warehouseType) === kind)
  if (matched) return String(matched.value)
  return String(warehouses[0].value)
}

function buildSystemCostLines(hints, categories = []) {
  const direct = hints?.directWage
  const overhead = hints?.overhead
  const directCat = categories.find((c) => c.code === 'DIRECT_WAGE' || Number(c.costType) === PRODUCTION_COST_TYPE.DirectWage)
  const overheadCat = categories.find((c) => c.code === 'OVERHEAD' || Number(c.costType) === PRODUCTION_COST_TYPE.Overhead)
  return [
    {
      productionCostCategoryId: String(
        direct?.productionCostCategoryId || directCat?.value || '',
      ),
      costType: String(PRODUCTION_COST_TYPE.DirectWage),
      description: direct?.description ?? 'هزینه تولید مستقیم',
      amountMode: String(direct?.amountMode ?? PRODUCTION_COST_AMOUNT_MODE.Flat),
      amount: direct?.amount ?? 0,
      isSystem: true,
      meta: direct ? `${direct.employeeCount ?? 0} نفر` : '',
    },
    {
      productionCostCategoryId: String(
        overhead?.productionCostCategoryId || overheadCat?.value || '',
      ),
      costType: String(PRODUCTION_COST_TYPE.Overhead),
      description: overhead?.description ?? 'هزینه تولید غیر مستقیم',
      amountMode: String(overhead?.amountMode ?? PRODUCTION_COST_AMOUNT_MODE.Flat),
      amount: overhead?.amount ?? 0,
      isSystem: true,
      meta: overhead ? `${overhead.employeeCount ?? 0} نفر` : '',
    },
  ]
}

function buildDefaultDynamicCostLines(categories = []) {
  return categories
    .filter((c) => !c.isSystem)
    .map((c) => ({
      productionCostCategoryId: String(c.value),
      costType: String(c.costType),
      description: c.label,
      amountMode: String(PRODUCTION_COST_AMOUNT_MODE.PerBase),
      amount: '',
      isSystem: false,
    }))
}

function mergeProductOptions(baseList, selectedId, fallbackLists = []) {
  if (!selectedId) return baseList
  const exists = baseList.some((item) => String(item.value) === String(selectedId))
  if (exists) return baseList
  for (const list of fallbackLists) {
    const found = list?.find((item) => String(item.value) === String(selectedId))
    if (found) return [found, ...baseList]
  }
  return baseList
}

function ProductionFormulasPage() {
  const tableRef = useRef(null)
  const formRef = useRef(null)
  const modalBodyRef = useRef(null)
  const nameInputRef = useRef(null)
  const { canCreate, canEdit, canDelete } = usePageCrud('/production/formulas')
  const [loadError, setLoadError] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editId, setEditId] = useState(null)
  const [deleteRow, setDeleteRow] = useState(null)
  const [formError, setFormError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [showAdvancedMode, setShowAdvancedMode] = useState(false)
  const [outputProducts, setOutputProducts] = useState([])
  const [materialProducts, setMaterialProducts] = useState([])
  const [allProducts, setAllProducts] = useState(null)
  const [outputShowAll, setOutputShowAll] = useState(false)
  const [materialShowAll, setMaterialShowAll] = useState(false)
  const [meaurments, setMeaurments] = useState([])
  const [materialWarehouses, setMaterialWarehouses] = useState([])
  const [costCategories, setCostCategories] = useState([])
  const [systemCostHints, setSystemCostHints] = useState(null)
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

  useBootstrapTooltips(modalBodyRef, showForm, [
    form.productId,
    form.costLines.length,
    form.materialLines.length,
    showAdvancedMode,
  ])

  useEffect(() => {
    if (!showForm) return undefined
    const timer = window.setTimeout(() => {
      nameInputRef.current?.focus()
      nameInputRef.current?.select?.()
    }, 50)
    return () => window.clearTimeout(timer)
  }, [showForm, editId])

  const ensureAllProducts = useCallback(async () => {
    if (allProducts) return allProducts
    const items = await fetchProductOptions()
    setAllProducts(items)
    return items
  }, [allProducts])

  useEffect(() => {
    fetchProductOptions({ kinds: [PRODUCT_KIND.Processed] })
      .then(setOutputProducts)
      .catch(() => setOutputProducts([]))
    fetchProductOptions({ kinds: [PRODUCT_KIND.Raw, PRODUCT_KIND.SemiFinished] })
      .then(setMaterialProducts)
      .catch(() => setMaterialProducts([]))
    fetchMeaurmentOptions().then(setMeaurments).catch(() => setMeaurments([]))
    fetchProductionMaterialWarehouses()
      .then(setMaterialWarehouses)
      .catch(() => setMaterialWarehouses([]))
    productionFormulasApi
      .fetchSystemCostHints()
      .then(setSystemCostHints)
      .catch(() => setSystemCostHints(null))
    fetchProductionCostCategoryOptions()
      .then(setCostCategories)
      .catch(() => setCostCategories([]))
  }, [])

  const dynamicCostCategories = useMemo(
    () => costCategories.filter((c) => !c.isSystem),
    [costCategories],
  )

  const productLookup = useMemo(() => {
    const map = new Map()
    for (const list of [outputProducts, materialProducts, allProducts ?? []]) {
      for (const item of list) {
        map.set(String(item.value), item)
      }
    }
    return map
  }, [allProducts, materialProducts, outputProducts])

  const outputSelectOptions = useMemo(() => {
    const base = outputShowAll && allProducts ? allProducts : outputProducts
    const merged = mergeProductOptions(base, form.productId, [allProducts, materialProducts, outputProducts])
    return outputShowAll ? merged : [SHOW_ALL_OPTION, ...merged]
  }, [allProducts, form.productId, materialProducts, outputProducts, outputShowAll])

  const materialSelectOptions = useMemo(() => {
    const base = materialShowAll && allProducts ? allProducts : materialProducts
    const selectedIds = form.materialLines.map((line) => line.productId).filter(Boolean)
    let merged = base
    for (const id of selectedIds) {
      merged = mergeProductOptions(merged, id, [allProducts, outputProducts, materialProducts])
    }
    return materialShowAll ? merged : [SHOW_ALL_OPTION, ...merged]
  }, [allProducts, form.materialLines, materialProducts, materialShowAll, outputProducts])

  const meaurmentsForProduct = useCallback(
    (productId) => {
      const product = productLookup.get(String(productId))
      if (!product?.baseMeaurmentId) return meaurments
      return meaurments.filter(
        (item) => item.baseMeaurmentId === product.baseMeaurmentId || item.value === product.baseMeaurmentId,
      )
    },
    [meaurments, productLookup],
  )

  const reloadTable = useCallback(() => tableRef.current?.dt()?.ajax.reload(null, false), [])

  const closeModals = useCallback(() => {
    setShowForm(false)
    setEditId(null)
    setDeleteRow(null)
    setFormError('')
    setSubmitting(false)
    setShowAdvancedMode(false)
    setOutputShowAll(false)
    setMaterialShowAll(false)
  }, [])

  const openCreate = useCallback(async () => {
    let hints = systemCostHints
    let categories = costCategories
    try {
      ;[hints, categories] = await Promise.all([
        productionFormulasApi.fetchSystemCostHints(),
        fetchProductionCostCategoryOptions(),
      ])
      setSystemCostHints(hints)
      setCostCategories(categories)
    } catch {
      // در صورت خطا از کش قبلی برای پیش‌فرض استفاده می‌شود
    }
    setForm({
      name: '',
      productId: '',
      meaurmentId: '',
      baseQuantity: '1',
      mode: String(PRODUCTION_FORMULA_MODE.Fixed),
      isDefault: false,
      notes: '',
      materialLines: [{ ...emptyMaterialLine }],
      costLines: [
        ...buildSystemCostLines(hints, categories),
        ...buildDefaultDynamicCostLines(categories),
      ],
    })
    setEditId(null)
    setFormError('')
    setShowAdvancedMode(false)
    setOutputShowAll(false)
    setMaterialShowAll(false)
    setShowForm(true)
  }, [costCategories, systemCostHints])

  const openEdit = useCallback(async (row) => {
    setFormError('')
    try {
      const formula = await productionFormulasApi.getById(row.productionFormulaId)
      const mode = String(formula.mode ?? PRODUCTION_FORMULA_MODE.Fixed)
      const hints = systemCostHints ?? (await productionFormulasApi.fetchSystemCostHints().catch(() => null))
      if (hints) setSystemCostHints(hints)

      const systemLines = buildSystemCostLines(hints, costCategories)
      const savedCosts = formula.costLines ?? []
      const mergedSystem = systemLines.map((sys) => {
        const saved = savedCosts.find((line) => Number(line.costType) === Number(sys.costType))
        return saved
          ? {
              ...sys,
              productionCostCategoryId: String(
                saved.productionCostCategoryId || sys.productionCostCategoryId || '',
              ),
              description: saved.description || sys.description,
              amountMode: String(saved.amountMode ?? sys.amountMode),
              amount: saved.amount,
            }
          : sys
      })
      const dynamicCosts = savedCosts
        .filter(
          (line) =>
            Number(line.costType) !== PRODUCTION_COST_TYPE.DirectWage &&
            Number(line.costType) !== PRODUCTION_COST_TYPE.Overhead,
        )
        .map((line) => ({
          productionCostCategoryId: String(line.productionCostCategoryId ?? ''),
          costType: String(line.costType),
          description: line.description ?? line.costCategoryName ?? '',
          amountMode: String(line.amountMode ?? PRODUCTION_COST_AMOUNT_MODE.PerBase),
          amount: line.amount,
          isSystem: false,
        }))

      const productId = formula.productId ?? ''
      const isProcessed = outputProducts.some((item) => String(item.value) === String(productId))
      if (!isProcessed) {
        await ensureAllProducts()
        setOutputShowAll(true)
      }

      const materialIds = (formula.materialLines ?? []).map((line) => line.productId)
      const needsAllMaterials = materialIds.some(
        (id) => !materialProducts.some((item) => String(item.value) === String(id)),
      )
      if (needsAllMaterials) {
        await ensureAllProducts()
        setMaterialShowAll(true)
      }

      setForm({
        name: formula.name ?? '',
        productId,
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
        costLines: [...mergedSystem, ...dynamicCosts],
      })
      setShowAdvancedMode(Number(mode) === PRODUCTION_FORMULA_MODE.Variable)
      setEditId(formula.productionFormulaId)
      setShowForm(true)
    } catch (error) {
      setLoadError(error.message)
      showAppToast(error.message)
    }
  }, [costCategories, ensureAllProducts, materialProducts, outputProducts, systemCostHints])

  const handleOutputProductChange = async (value) => {
    if (value === SHOW_ALL_VALUE) {
      try {
        await ensureAllProducts()
        setOutputShowAll(true)
      } catch (error) {
        showAppToast(error.message || 'بارگذاری محصولات ناموفق بود.')
      }
      return
    }

    const product = productLookup.get(String(value))
      ?? (await ensureAllProducts()).find((item) => String(item.value) === String(value))
    setForm((prev) => ({
      ...prev,
      productId: value,
      meaurmentId: resolveDefaultUnit(product),
    }))
  }

  const handleMaterialProductChange = async (index, value) => {
    if (value === SHOW_ALL_VALUE) {
      try {
        await ensureAllProducts()
        setMaterialShowAll(true)
      } catch (error) {
        showAppToast(error.message || 'بارگذاری محصولات ناموفق بود.')
      }
      return
    }

    const product = productLookup.get(String(value))
      ?? (await ensureAllProducts()).find((item) => String(item.value) === String(value))
    setForm((prev) => ({
      ...prev,
      materialLines: prev.materialLines.map((line, lineIndex) => (
        lineIndex === index
          ? {
              ...line,
              productId: value,
              meaurmentId: resolveDefaultUnit(product),
              defaultWarehouseId: resolveDefaultWarehouse(product, materialWarehouses),
            }
          : line
      )),
    }))
  }

  const updateLine = (section, index, name, value) => {
    setForm((prev) => ({
      ...prev,
      [section]: prev[section].map((line, lineIndex) => (
        lineIndex === index ? { ...line, [name]: value } : line
      )),
    }))
  }

  const addMaterialLine = () => {
    setForm((prev) => ({ ...prev, materialLines: [...prev.materialLines, { ...emptyMaterialLine }] }))
  }

  const addDynamicCostLine = () => {
    setForm((prev) => ({
      ...prev,
      costLines: [...prev.costLines, emptyDynamicCostLine(dynamicCostCategories)],
    }))
  }

  const handleCostCategoryChange = (index, categoryId) => {
    const cat = dynamicCostCategories.find((c) => String(c.value) === String(categoryId))
    setForm((prev) => ({
      ...prev,
      costLines: prev.costLines.map((line, lineIndex) => (
        lineIndex === index
          ? {
              ...line,
              productionCostCategoryId: categoryId,
              costType: String(cat?.costType ?? line.costType),
              description: cat?.label || line.description,
            }
          : line
      )),
    }))
  }

  const removeLine = (section, index) => {
    setForm((prev) => ({
      ...prev,
      [section]: prev[section].filter((_, lineIndex) => lineIndex !== index),
    }))
  }

  const refreshSystemCosts = async () => {
    try {
      const hints = await productionFormulasApi.fetchSystemCostHints()
      setSystemCostHints(hints)
      const refreshed = buildSystemCostLines(hints, costCategories)
      setForm((prev) => {
        const dynamic = prev.costLines.filter((line) => !line.isSystem)
        return { ...prev, costLines: [...refreshed, ...dynamic] }
      })
      showAppToast('مقادیر پیش‌فرض سیستمی اعمال شد.', 'success')
    } catch (error) {
      showAppToast(error.message || 'به‌روزرسانی هزینه‌های سیستمی ناموفق بود.')
    }
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    const formEl = formRef.current
    if (formEl) {
      const message = validateFormPersian(formEl)
      if (message) {
        showAppToast(message)
        formEl.reportValidity()
        return
      }
    }

    setSubmitting(true)
    setFormError('')
    try {
      const payload = buildProductionFormulaPayload({
        ...form,
        mode: showAdvancedMode ? form.mode : String(PRODUCTION_FORMULA_MODE.Fixed),
      })
      if (editId) {
        await productionFormulasApi.update(editId, payload)
        showAppToast('فرمول با موفقیت ویرایش شد.', 'success')
      } else {
        await productionFormulasApi.create(payload)
        showAppToast('فرمول با موفقیت ایجاد شد.', 'success')
      }
      closeModals()
      reloadTable()
    } catch (error) {
      setFormError(error.message)
      setSubmitting(false)
      showAppToast(error.message)
    }
  }

  const handleSetDefault = useCallback(async (row) => {
    setLoadError('')
    try {
      await productionFormulasApi.setDefault(row.productionFormulaId)
      reloadTable()
      showAppToast('فرمول به‌عنوان پیش‌فرض تنظیم شد.', 'success')
    } catch (error) {
      setLoadError(error.message)
      showAppToast(error.message)
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
      showAppToast('فرمول حذف شد.', 'success')
    } catch (error) {
      setFormError(error.message)
      setSubmitting(false)
      showAppToast(error.message)
    }
  }

  useModalKeyboardShortcuts({
    open: showForm,
    onClose: closeModals,
    onSave: !submitting ? () => formRef.current?.requestSubmit() : undefined,
    formRef,
  })

  useModalKeyboardShortcuts({
    open: Boolean(deleteRow),
    onClose: closeModals,
  })

  usePageCreateShortcut({
    enabled: canCreate,
    onNew: openCreate,
    isBlocked: showForm || Boolean(deleteRow),
  })

  useModalAutoFocus({ open: showForm, formRef })

  const costTotal = useMemo(
    () => form.costLines.reduce((sum, line) => sum + (Number(line.amount) || 0), 0),
    [form.costLines],
  )

  const tableOptions = useMemo(
    () => ({
      processing: true,
      serverSide: true,
      ajax: productionFormulasApi.createDataTableAjax(setLoadError),
      paging: true,
      searching: true,
      ordering: true,
      info: true,
      scrollX: false,
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
            <button
              type="button"
              className="dt-action-btn"
              title="تنظیم به‌عنوان پیش‌فرض"
              onClick={() => handleSetDefault(row)}
            >
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
            <p className="text-muted mb-0 small">تعریف مواد مصرفی و هزینه‌های استاندارد تولید</p>
          </div>
          {canCreate && (
            <button
              type="button"
              className="btn btn-primary d-inline-flex align-items-center gap-2"
              onClick={openCreate}
              {...tipProps('میانبر: Ctrl+N / Ctrl+Space')}
            >
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
                <form ref={formRef} onSubmit={handleSubmit} noValidate>
                  <div className="modal-header">
                    <h5 className="modal-title">{editId ? 'ویرایش فرمول ساخت' : 'فرمول ساخت جدید'}</h5>
                    <button type="button" className="btn-close" onClick={closeModals} />
                  </div>
                  <div className="modal-body" ref={modalBodyRef}>
                    {formError && <div className="alert alert-danger">{formError}</div>}

                    <div className="row g-3 mb-4">
                      <div className="col-md-6">
                        <label className="form-label">نام فرمول</label>
                        <input
                          ref={nameInputRef}
                          className="form-control"
                          value={form.name}
                          required
                          {...persianValidity('لطفاً نام فرمول را وارد کنید.')}
                          onChange={(event) => {
                            event.target.setCustomValidity('')
                            setForm((prev) => ({ ...prev, name: event.target.value }))
                          }}
                        />
                      </div>
                      <div className="col-md-3">
                        <label className="form-label d-inline-flex align-items-center gap-1">
                          محصول خروجی
                          <span
                            className="text-muted"
                            role="img"
                            aria-label="راهنما"
                            {...tipProps('پیش‌فرض: محصولات پروسس‌شده. برای دیدن همه، گزینه «نمایش همه محصولات» را انتخاب کنید.')}
                          >
                            <Icon name="circle-info" className="small" />
                          </span>
                        </label>
                        <SearchableSelect
                          options={outputSelectOptions}
                          value={form.productId}
                          required
                          requiredMessage="لطفاً محصول خروجی را انتخاب کنید."
                          onChange={handleOutputProductChange}
                        />
                      </div>
                      <div className="col-md-3">
                        <label className="form-label d-inline-flex align-items-center gap-1">
                          واحد
                          <span
                            className="text-muted"
                            role="img"
                            aria-label="راهنما"
                            {...tipProps('با انتخاب محصول، واحد پیش‌فرض آن به‌صورت خودکار انتخاب می‌شود.')}
                          >
                            <Icon name="circle-info" className="small" />
                          </span>
                        </label>
                        <select
                          className="form-select"
                          value={form.meaurmentId}
                          required
                          {...persianValidity('لطفاً واحد را انتخاب کنید.')}
                          onChange={(event) => {
                            event.target.setCustomValidity('')
                            setForm((prev) => ({ ...prev, meaurmentId: event.target.value }))
                          }}
                        >
                          <option value="">—</option>
                          {meaurmentsForProduct(form.productId).map((item) => (
                            <option key={item.value} value={item.value}>{item.label}</option>
                          ))}
                        </select>
                      </div>
                      <div className="col-md-3">
                        <label className="form-label d-inline-flex align-items-center gap-1">
                          مقدار پایه
                          <span
                            className="text-muted"
                            role="img"
                            aria-label="راهنما"
                            {...tipProps('فرمول برای این مقدار خروجی تعریف می‌شود؛ مواد و هزینه‌های «به ازای پایه» نسبت به آن مقیاس می‌گیرند.')}
                          >
                            <Icon name="circle-info" className="small" />
                          </span>
                        </label>
                        <input
                          type="number"
                          min="0.000001"
                          step="any"
                          className="form-control"
                          value={form.baseQuantity}
                          required
                          {...persianValidity('لطفاً مقدار پایه معتبر وارد کنید.')}
                          onChange={(event) => {
                            event.target.setCustomValidity('')
                            setForm((prev) => ({ ...prev, baseQuantity: event.target.value }))
                          }}
                        />
                      </div>
                      <div className="col-md-3 d-flex align-items-end">
                        <div className="form-check mb-2">
                          <input
                            id="formula-default"
                            type="checkbox"
                            className="form-check-input"
                            checked={form.isDefault}
                            onChange={(event) => setForm((prev) => ({ ...prev, isDefault: event.target.checked }))}
                          />
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
                          <select
                            className="form-select"
                            value={form.mode}
                            onChange={(event) => setForm((prev) => ({ ...prev, mode: event.target.value }))}
                          >
                            {PRODUCTION_FORMULA_MODE_OPTIONS.map((item) => (
                              <option key={item.value} value={item.value}>{item.label}</option>
                            ))}
                          </select>
                        </div>
                      )}
                      <div className="col-12">
                        <label className="form-label">یادداشت</label>
                        <input
                          className="form-control"
                          value={form.notes}
                          onChange={(event) => setForm((prev) => ({ ...prev, notes: event.target.value }))}
                        />
                      </div>
                    </div>

                    <div className="d-flex justify-content-between align-items-center mb-2">
                      <h6 className="mb-0 d-inline-flex align-items-center gap-1">
                        مواد مصرفی
                        <span
                          className="text-muted"
                          role="img"
                          aria-label="راهنما"
                          {...tipProps('پیش‌فرض: مواد خام و نیمه‌پروسس. انبار و واحد با انتخاب محصول پیشنهاد می‌شوند.')}
                        >
                          <Icon name="circle-info" className="small" />
                        </span>
                      </h6>
                      <button type="button" className="btn btn-sm btn-outline-secondary" onClick={addMaterialLine}>
                        <Icon name="plus" /> ردیف ماده
                      </button>
                    </div>
                    <div className="table-responsive mb-4">
                      <table className="table table-sm align-middle production-lines-table">
                        <thead>
                          <tr>
                            <th>محصول</th>
                            <th>واحد</th>
                            <th>مقدار برای پایه</th>
                            <th>انبار پیش‌فرض</th>
                            <th />
                          </tr>
                        </thead>
                        <tbody>
                          {form.materialLines.map((line, index) => (
                            <tr key={`material-${index}`}>
                                <td>
                                <SearchableSelect
                                  options={materialSelectOptions}
                                  value={line.productId}
                                  size="sm"
                                  required
                                  requiredMessage="لطفاً ماده مصرفی را انتخاب کنید."
                                  onChange={(value) => handleMaterialProductChange(index, value)}
                                />
                              </td>
                              <td>
                                <select
                                  className="form-select form-select-sm"
                                  value={line.meaurmentId}
                                  required
                                  {...persianValidity('لطفاً واحد ماده را انتخاب کنید.')}
                                  onChange={(event) => {
                                    event.target.setCustomValidity('')
                                    updateLine('materialLines', index, 'meaurmentId', event.target.value)
                                  }}
                                >
                                  <option value="">—</option>
                                  {meaurmentsForProduct(line.productId).map((item) => (
                                    <option key={item.value} value={item.value}>{item.label}</option>
                                  ))}
                                </select>
                              </td>
                              <td>
                                <input
                                  type="number"
                                  min="0.000001"
                                  step="any"
                                  className="form-control form-control-sm"
                                  value={line.quantity}
                                  required
                                  {...persianValidity('لطفاً مقدار ماده را وارد کنید.')}
                                  onChange={(event) => {
                                    event.target.setCustomValidity('')
                                    updateLine('materialLines', index, 'quantity', event.target.value)
                                  }}
                                />
                              </td>
                              <td>
                                <SearchableSelect
                                  options={materialWarehouses}
                                  value={line.defaultWarehouseId}
                                  size="sm"
                                  onChange={(value) => updateLine('materialLines', index, 'defaultWarehouseId', value)}
                                />
                              </td>
                              <td>
                                <button
                                  type="button"
                                  className="btn btn-sm btn-outline-danger"
                                  disabled={form.materialLines.length === 1}
                                  onClick={() => removeLine('materialLines', index)}
                                >
                                  <Icon name="trash" />
                                </button>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>

                    <div className="d-flex justify-content-between align-items-center mb-2 flex-wrap gap-2">
                      <h6 className="mb-0 d-inline-flex align-items-center gap-1">
                        هزینه‌های تولید
                        <span
                          className="text-muted"
                          role="img"
                          aria-label="راهنما"
                          {...tipProps('مستقیم و غیرمستقیم از حقوق پایه بخش‌های انتخاب‌شده پر می‌شوند و قابل ویرایش‌اند. دسته‌های دیگر را از «دسته‌بندی‌ها ← هزینه‌های تولید» مدیریت کنید.')}
                        >
                          <Icon name="circle-info" className="small" />
                        </span>
                      </h6>
                      <div className="d-flex gap-2">
                        <button
                          type="button"
                          className="btn btn-sm btn-outline-primary"
                          onClick={refreshSystemCosts}
                          {...tipProps('مقادیر پیش‌فرض سیستمی را دوباره اعمال می‌کند.')}
                        >
                          <Icon name="rotate-left" /> به‌روزرسانی سیستمی
                        </button>
                        <button type="button" className="btn btn-sm btn-outline-secondary" onClick={addDynamicCostLine}>
                          <Icon name="plus" /> ردیف هزینه
                        </button>
                      </div>
                    </div>
                    <div className="table-responsive">
                      <table className="table table-sm align-middle production-lines-table">
                        <thead>
                          <tr>
                            <th>دسته هزینه</th>
                            <th>شرح</th>
                            <th>روش محاسبه</th>
                            <th>مبلغ</th>
                            <th />
                          </tr>
                        </thead>
                        <tbody>
                          {form.costLines.map((line, index) => (
                            <tr key={`cost-${index}`}>
                              <td>
                                {line.isSystem ? (
                                  <div className="d-flex flex-column">
                                    <span>{costCategoryLabel(costCategories, line)}</span>
                                    {line.meta && <small className="text-muted">{line.meta}</small>}
                                  </div>
                                ) : (
                                  <select
                                    className="form-select form-select-sm"
                                    value={line.productionCostCategoryId}
                                    required
                                    {...persianValidity('لطفاً دسته هزینه را انتخاب کنید.')}
                                    onChange={(event) => {
                                      event.target.setCustomValidity('')
                                      handleCostCategoryChange(index, event.target.value)
                                    }}
                                  >
                                    <option value="">—</option>
                                    {dynamicCostCategories.map((item) => (
                                      <option key={item.value} value={item.value}>{item.label}</option>
                                    ))}
                                  </select>
                                )}
                              </td>
                              <td>
                                <input
                                  className="form-control form-control-sm"
                                  value={line.description}
                                  onChange={(event) => updateLine('costLines', index, 'description', event.target.value)}
                                />
                              </td>
                              <td>
                                <select
                                  className="form-select form-select-sm"
                                  value={line.amountMode}
                                  onChange={(event) => updateLine('costLines', index, 'amountMode', event.target.value)}
                                >
                                  {PRODUCTION_COST_AMOUNT_MODE_OPTIONS.map((item) => (
                                    <option key={item.value} value={item.value}>{item.label}</option>
                                  ))}
                                </select>
                              </td>
                              <td>
                                <AmountField
                                  value={line.amount}
                                  className="amount-field-sm"
                                  min="0"
                                  onChange={(value) => updateLine('costLines', index, 'amount', value)}
                                />
                              </td>
                              <td>
                                {!line.isSystem && (
                                  <button
                                    type="button"
                                    className="btn btn-sm btn-outline-danger"
                                    onClick={() => removeLine('costLines', index)}
                                  >
                                    <Icon name="trash" />
                                  </button>
                                )}
                              </td>
                            </tr>
                          ))}
                          {form.costLines.length === 0 && (
                            <tr>
                              <td colSpan="5" className="text-center text-muted">هزینه‌ای تعریف نشده است.</td>
                            </tr>
                          )}
                        </tbody>
                      </table>
                    </div>
                    {form.costLines.length > 0 && (
                      <div className="d-flex justify-content-between align-items-center mt-2 small text-muted">
                        <span>جمع هزینه‌های فرمول</span>
                        <span className="fw-semibold text-body">{formatAmount(costTotal)}</span>
                      </div>
                    )}
                  </div>
                  <div className="modal-footer">
                    <button type="button" className="btn btn-secondary" onClick={closeModals}>انصراف</button>
                    <button type="submit" className="btn btn-primary" disabled={submitting}>
                      {submitting ? 'در حال ذخیره...' : 'ذخیره'}
                    </button>
                  </div>
                </form>
              </div>
            </div>
          </div>
        )}

        {deleteRow && (
          <div className="modal show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
            <div className="modal-dialog">
              <div className="modal-content">
                <div className="modal-header">
                  <h5 className="modal-title">حذف فرمول ساخت</h5>
                  <button type="button" className="btn-close" onClick={closeModals} />
                </div>
                <div className="modal-body">
                  {formError && <div className="alert alert-danger">{formError}</div>}
                  <p>فرمول «{deleteRow.name}» حذف شود؟</p>
                </div>
                <div className="modal-footer">
                  <button type="button" className="btn btn-secondary" onClick={closeModals}>انصراف</button>
                  <button type="button" className="btn btn-danger" disabled={submitting} onClick={handleDeleteConfirm}>
                    حذف
                  </button>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

export default ProductionFormulasPage
