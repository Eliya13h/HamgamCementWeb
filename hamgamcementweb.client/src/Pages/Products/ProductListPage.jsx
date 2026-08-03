import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import Icon from '../../components/common/Icon'
import DataTable from '../../lib/dataTableSetup'
import { showAppToast } from '../../lib/appToast'
import { persianValidity, validateFormPersian } from '../../lib/persianFormValidity'
import { useModalKeyboardShortcuts, usePageCreateShortcut } from '../../hooks/useModalKeyboardShortcuts'
import { tipProps, useBootstrapTooltips } from '../../hooks/useBootstrapTooltips'
import { usePageCrud } from '../../permissions/usePageCrud'
import { fetchBaseCurrency } from '../../services/currenciesApi'
import {
  fetchCategoryOptions,
  fetchBaseMeaurmentOptions,
  fetchMeaurmentOptions,
  fetchNextProductCodePreview,
  productsApi,
  PRODUCT_KIND,
  PRODUCT_KIND_OPTIONS,
  PRODUCT_SALE_PRICE_MODE,
  PRODUCT_SALE_PRICE_MODE_OPTIONS,
} from '../../services/productsApi'
import { dataTableLanguage, formatAmount } from '../Transport/CrudTablePage'

/** مقدار واحد → مقدار پایه (FactorToBase) */
function quantityToBase(quantity, factorToBase) {
  const qty = Number(quantity)
  const factor = Number(factorToBase)
  if (!Number.isFinite(qty)) return 0
  if (!(factor > 0)) return qty
  return qty * factor
}

/** مقدار پایه → مقدار واحد */
function quantityFromBase(quantityInBase, factorToBase) {
  const qty = Number(quantityInBase)
  const factor = Number(factorToBase)
  if (!Number.isFinite(qty)) return 0
  if (!(factor > 0)) return qty
  return qty / factor
}

function getMeaurmentFactor(options, meaurmentId) {
  if (!meaurmentId) return 1
  const found = options.find((m) => Number(m.value) === Number(meaurmentId))
  const factor = Number(found?.factorToBase)
  return factor > 0 ? factor : 1
}

function convertMinStockBetweenUnits(value, fromFactor, toFactor) {
  if (value === '' || value == null) return ''
  const inBase = quantityToBase(value, fromFactor)
  const converted = quantityFromBase(inBase, toFactor)
  if (!Number.isFinite(converted)) return ''
  // حداکثر ۶ رقم اعشار برای جلوگیری از نویز اعشار
  return String(Math.round(converted * 1e6) / 1e6)
}

function MultiCheckboxGroup({ label, tip, options, selected, onChange, disabled }) {
  return (
    <div>
      <label className="form-label mb-1">{label}</label>
      <div
        className="d-flex flex-wrap gap-3 border rounded-3 p-3 bg-body-tertiary"
        tabIndex={0}
        {...tipProps(tip)}
      >
        {options.length === 0 ? (
          <span className="text-muted small">
            {disabled ? 'ابتدا واحد پایه را انتخاب کنید' : 'گزینه‌ای موجود نیست'}
          </span>
        ) : (
          options.map((option) => (
            <div className="form-check" key={option.value}>
              <input
                className="form-check-input"
                type="checkbox"
                id={`chk-meaurment-${option.value}`}
                checked={selected.includes(option.value)}
                disabled={disabled}
                onChange={(e) => {
                  if (e.target.checked) {
                    onChange([...selected, option.value])
                  } else {
                    onChange(selected.filter((id) => id !== option.value))
                  }
                }}
              />
              <label className="form-check-label" htmlFor={`chk-meaurment-${option.value}`}>
                {option.label}
              </label>
            </div>
          ))
        )}
      </div>
    </div>
  )
}

const columns = [
  { data: 'code', title: 'کد' },
  { data: 'name', title: 'نام' },
  { data: 'productKindText', title: 'نوع' },
  { data: 'categoriesText', title: 'دسته‌بندی', orderable: false },
  {
    data: 'suggestedPurchasePrice',
    title: 'بهای لحظه‌ای',
    orderable: false,
    className: 'text-end',
    render: (data, type) => {
      if (type !== 'display') return data
      if (data == null || data === '') return '—'
      return formatAmount(data)
    },
  },
  {
    data: 'defaultSalePrice',
    title: 'قیمت فروش پیشنهادی',
    className: 'text-end',
    render: (data, type, row) => {
      if (type !== 'display') return data
      if (Number(row.salePriceMode) === PRODUCT_SALE_PRICE_MODE.ProfitPercent) {
        return `٪${formatAmount(row.saleProfitPercent)} سود`
      }
      return formatAmount(data)
    },
  },
  {
    data: 'totalStockQuantity',
    title: 'موجودی کل',
    orderable: false,
    className: 'text-end',
    render: (data, type, row) => {
      if (type !== 'display') return data
      const qty = formatAmount(data)
      const unit = row.baseMeaurmentName ? ` ${row.baseMeaurmentName}` : ''
      if (row.isBelowMinStock) {
        return `<span class="text-danger fw-semibold" title="موجودی کمتر از حداقل (${formatAmount(row.minStockQuantity)}${unit})">${qty}${unit} <span aria-hidden="true">⚠</span></span>`
      }
      return `${qty}${unit}`
    },
  },
]

const emptyForm = {
  codePreview: '',
  name: '',
  description: '',
  productKind: PRODUCT_KIND.Processed,
  baseMeaurmentId: '',
  defaultMeaurmentId: '',
  salePriceMode: PRODUCT_SALE_PRICE_MODE.Fixed,
  defaultSalePrice: '',
  saleProfitPercent: '',
  minStockQuantity: '',
  categoryId: '',
  meaurmentIds: [],
  isActive: true,
  isProductKindLocked: false,
}

function ProductListPage() {
  const { canCreate, canEdit, canDelete } = usePageCrud('/products/list')
  const tableRef = useRef(null)
  const formRef = useRef(null)
  const nameInputRef = useRef(null)
  const [showCreate, setShowCreate] = useState(false)
  const [editRow, setEditRow] = useState(null)
  const [deleteRow, setDeleteRow] = useState(null)
  const [submitting, setSubmitting] = useState(false)
  const [form, setForm] = useState(emptyForm)
  const [categoryOptions, setCategoryOptions] = useState([])
  const [baseUnitOptions, setBaseUnitOptions] = useState([])
  const [meaurmentOptions, setMeaurmentOptions] = useState([])
  const [baseCurrencySymbol, setBaseCurrencySymbol] = useState('')

  useEffect(() => {
    let cancelled = false
    async function loadOptions() {
      try {
        const [categories, baseUnits, baseCurrency] = await Promise.all([
          fetchCategoryOptions(),
          fetchBaseMeaurmentOptions(),
          fetchBaseCurrency().catch(() => null),
        ])
        if (!cancelled) {
          setCategoryOptions(categories ?? [])
          setBaseUnitOptions(baseUnits ?? [])
          setBaseCurrencySymbol(baseCurrency?.symbol ?? '')
        }
      } catch (error) {
        showAppToast(error.message || 'بارگذاری گزینه‌ها با خطا مواجه شد.')
      }
    }
    loadOptions()
    return () => {
      cancelled = true
    }
  }, [])

  const loadMeaurmentsForBase = useCallback(async (baseMeaurmentId) => {
    if (!baseMeaurmentId) {
      setMeaurmentOptions([])
      return []
    }
    const items = await fetchMeaurmentOptions(baseMeaurmentId)
    setMeaurmentOptions(items ?? [])
    return items ?? []
  }, [])

  useEffect(() => {
    if (!form.baseMeaurmentId) {
      setMeaurmentOptions([])
      return
    }
    loadMeaurmentsForBase(form.baseMeaurmentId).catch((error) => {
      showAppToast(error.message || 'بارگذاری واحدها با خطا مواجه شد.')
    })
  }, [form.baseMeaurmentId, loadMeaurmentsForBase])

  const defaultMeaurmentChoices = useMemo(
    () => meaurmentOptions.filter((m) => form.meaurmentIds.includes(m.value)),
    [meaurmentOptions, form.meaurmentIds],
  )

  const selectedDefaultUnitLabel = useMemo(() => {
    const id = form.defaultMeaurmentId || form.baseMeaurmentId
    return (
      meaurmentOptions.find((option) => Number(option.value) === Number(id))?.label ||
      baseUnitOptions.find((option) => Number(option.value) === Number(form.baseMeaurmentId))
        ?.label ||
      ''
    )
  }, [
    meaurmentOptions,
    baseUnitOptions,
    form.defaultMeaurmentId,
    form.baseMeaurmentId,
  ])

  const defaultUnitFactor = useMemo(
    () =>
      getMeaurmentFactor(
        meaurmentOptions,
        form.defaultMeaurmentId || form.baseMeaurmentId,
      ),
    [meaurmentOptions, form.defaultMeaurmentId, form.baseMeaurmentId],
  )

  const reloadTable = useCallback(() => {
    tableRef.current?.dt()?.ajax.reload(null, false)
  }, [])

  const openCreate = useCallback(() => {
    // مثل صفحات دیگر: مدال را فوری باز کن تا Ctrl+Space منتظر API نماند
    setForm({ ...emptyForm })
    setShowCreate(true)
    fetchNextProductCodePreview()
      .then((preview) => {
        setForm((prev) => ({ ...prev, codePreview: preview?.code ?? '' }))
      })
      .catch((error) => {
        showAppToast(error.message || 'دریافت کد محصول با خطا مواجه شد.')
      })
  }, [])

  const openEdit = async (row) => {
    try {
      const detail = await productsApi.getById(row.productId)
      const units = await loadMeaurmentsForBase(detail.baseMeaurmentId)
      const defaultId = detail.defaultMeaurmentId || detail.baseMeaurmentId
      const factor = getMeaurmentFactor(units, defaultId)
      const minInDefault =
        detail.minStockQuantity == null || detail.minStockQuantity === ''
          ? ''
          : convertMinStockBetweenUnits(detail.minStockQuantity, 1, factor)

      setForm({
        codePreview: detail.code ?? '',
        name: detail.name ?? '',
        description: detail.description ?? '',
        productKind: detail.productKind ?? PRODUCT_KIND.Processed,
        baseMeaurmentId: detail.baseMeaurmentId ?? '',
        defaultMeaurmentId: detail.defaultMeaurmentId ?? '',
        salePriceMode: detail.salePriceMode ?? PRODUCT_SALE_PRICE_MODE.Fixed,
        defaultSalePrice: detail.defaultSalePrice ?? '',
        saleProfitPercent: detail.saleProfitPercent ?? '',
        // در فرم به واحد پیش‌فرض؛ در دیتابیس به واحد پایه
        minStockQuantity: minInDefault,
        categoryId: detail.categoryIds?.[0] ?? '',
        meaurmentIds: detail.meaurmentIds ?? [],
        isActive: detail.isActive ?? true,
        isProductKindLocked: detail.isProductKindLocked === true,
      })
      setEditRow(row)
    } catch (error) {
      showAppToast(error.message || 'بارگذاری محصول با خطا مواجه شد.')
    }
  }

  const closeModals = useCallback(() => {
    setShowCreate(false)
    setEditRow(null)
    setDeleteRow(null)
    setSubmitting(false)
  }, [])

  const modalOpen = showCreate || Boolean(editRow) || Boolean(deleteRow)
  const formModalOpen = showCreate || Boolean(editRow)
  const canSaveForm = formModalOpen

  useEffect(() => {
    if (!formModalOpen) return undefined
    const timer = window.setTimeout(() => {
      nameInputRef.current?.focus()
      nameInputRef.current?.select?.()
    }, 50)
    return () => window.clearTimeout(timer)
  }, [formModalOpen, showCreate, editRow])

  // فقط وقتی ساختار مدال عوض می‌شود (نه با هر تغییر واحد/لیست واحدها)
  useBootstrapTooltips(formRef, formModalOpen, [
    form.salePriceMode,
    form.isProductKindLocked,
    Boolean(editRow),
  ])

  const handleSubmit = useCallback(
    async (event) => {
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

      if (form.meaurmentIds.length === 0) {
        showAppToast('حداقل یک واحد اندازه‌گیری برای محصول انتخاب کنید.')
        return
      }

      setSubmitting(true)

      const salePriceMode = Number(form.salePriceMode)
      const payload = {
        name: form.name.trim(),
        description: form.description.trim() || null,
        productKind: Number(form.productKind),
        baseMeaurmentId: Number(form.baseMeaurmentId),
        defaultMeaurmentId: form.defaultMeaurmentId
          ? Number(form.defaultMeaurmentId)
          : null,
        salePriceMode,
        defaultSalePrice:
          salePriceMode === PRODUCT_SALE_PRICE_MODE.Fixed
            ? form.defaultSalePrice === ''
              ? 0
              : Number(form.defaultSalePrice)
            : 0,
        saleProfitPercent:
          salePriceMode === PRODUCT_SALE_PRICE_MODE.ProfitPercent
            ? form.saleProfitPercent === ''
              ? 0
              : Number(form.saleProfitPercent)
            : 0,
        minStockQuantity:
          form.minStockQuantity === ''
            ? 0
            : quantityToBase(form.minStockQuantity, defaultUnitFactor),
        categoryIds: form.categoryId ? [Number(form.categoryId)] : [],
        meaurmentIds: form.meaurmentIds,
        isActive: editRow ? form.isActive : true,
      }

      try {
        if (editRow) {
          await productsApi.update(editRow.productId, payload)
        } else {
          await productsApi.create(payload)
        }
        closeModals()
        reloadTable()
        showAppToast(editRow ? 'محصول با موفقیت ویرایش شد.' : 'محصول با موفقیت ایجاد شد.', 'success')
      } catch (error) {
        showAppToast(error.message || 'ذخیره محصول با خطا مواجه شد.')
        setSubmitting(false)
      }
    },
    [form, editRow, closeModals, reloadTable, defaultUnitFactor],
  )

  const triggerSave = useCallback(() => {
    if (!submitting) {
      formRef.current?.requestSubmit()
    }
  }, [submitting])

  useModalKeyboardShortcuts({
    open: formModalOpen,
    onClose: closeModals,
    onSave: canSaveForm ? triggerSave : undefined,
    formRef,
  })

  useModalKeyboardShortcuts({
    open: Boolean(deleteRow),
    onClose: closeModals,
  })

  usePageCreateShortcut({
    enabled: canCreate,
    onNew: openCreate,
    isBlocked: modalOpen,
  })

  const handleBaseUnitChange = (baseMeaurmentId) => {
    setForm((prev) => ({
      ...prev,
      baseMeaurmentId,
      meaurmentIds: [],
      defaultMeaurmentId: '',
    }))
  }

  const handleDeleteConfirm = async () => {
    if (!deleteRow) return
    setSubmitting(true)
    try {
      await productsApi.remove(deleteRow.productId)
      closeModals()
      reloadTable()
      showAppToast('محصول با موفقیت حذف شد.', 'success')
    } catch (error) {
      showAppToast(error.message || 'حذف محصول با خطا مواجه شد.')
      setSubmitting(false)
    }
  }

  const actionsIndex = columns.length + 1
  const isFixedSalePrice = Number(form.salePriceMode) === PRODUCT_SALE_PRICE_MODE.Fixed

  const tableOptions = useMemo(
    () => ({
      processing: true,
      serverSide: true,
      ajax: productsApi.createDataTableAjax((message) => {
        if (message) showAppToast(message)
      }),
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
          orderable: col.orderable !== false,
          className: col.className,
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
          width: '100px',
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
          {canEdit && (
            <button
              type="button"
              className="dt-action-btn"
              title="ویرایش"
              onClick={() => openEdit(row)}
            >
              <Icon name="edit" />
            </button>
          )}
          {canDelete && (
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
    [actionsIndex, canEdit, canDelete],
  )

  const nameValidity = persianValidity('لطفاً نام محصول را وارد کنید')
  const baseUnitValidity = persianValidity('لطفاً واحد پایه را انتخاب کنید')
  const kindValidity = persianValidity('لطفاً نوع محصول را انتخاب کنید')
  const saleModeValidity = persianValidity('لطفاً حالت قیمت فروش را انتخاب کنید')
  const salePriceValidity = persianValidity('لطفاً قیمت فروش پیشنهادی را وارد کنید')
  const profitValidity = persianValidity('لطفاً درصد سود را وارد کنید')

  return (
    <div className="users-page">
      <div className="content-card card border-0 h-100">
        <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between gap-3 flex-wrap">
          <h2 className="card-title mb-0">لیست محصولات</h2>
          {canCreate && (
            <button
              type="button"
              className="btn btn-sm btn-accent btn-users-new d-inline-flex align-items-center gap-2"
              onClick={openCreate}
              title="محصول جدید (Ctrl+Space)"
            >
              <Icon name="plus" />
              <span>محصول جدید</span>
            </button>
          )}
        </div>

        <div className="card-body card-body-table">
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

      {(showCreate || editRow) && (
        <>
          <div className="modal-backdrop show users-modal-backdrop" onClick={closeModals} />
          <div className="modal show d-block users-modal" tabIndex="-1" role="dialog">
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable modal-lg">
              <form className="modal-content" ref={formRef} onSubmit={handleSubmit} noValidate>
                <div className="modal-header">
                  <h5 className="modal-title">
                    {editRow ? 'ویرایش محصول' : 'محصول جدید'}
                  </h5>
                  <button
                    type="button"
                    className="btn-close"
                    aria-label="بستن"
                    onClick={closeModals}
                  />
                </div>
                <div className="modal-body">
                  <div className="row g-3">
                    <div className="col-md-4">
                      <label className="form-label mb-1" htmlFor="product-code">
                        کد محصول
                      </label>
                      <span
                        className="d-block"
                        tabIndex={0}
                        {...tipProps('کد پس از ذخیره به‌صورت خودکار ثبت می‌شود')}
                      >
                        <input
                          id="product-code"
                          className="form-control"
                          value={form.codePreview}
                          readOnly
                          disabled
                          tabIndex={-1}
                        />
                      </span>
                    </div>
                    <div className="col-md-8">
                      <label className="form-label mb-1" htmlFor="product-name">
                        نام محصول
                      </label>
                      <input
                        id="product-name"
                        ref={nameInputRef}
                        className="form-control"
                        value={form.name}
                        required
                        {...nameValidity}
                        onChange={(e) => {
                          e.target.setCustomValidity('')
                          setForm({ ...form, name: e.target.value })
                        }}
                      />
                    </div>

                    <div className="col-12">
                      <label className="form-label mb-1" htmlFor="product-description">
                        توضیحات
                      </label>
                      <textarea
                        id="product-description"
                        className="form-control"
                        rows={2}
                        value={form.description}
                        onChange={(e) => setForm({ ...form, description: e.target.value })}
                      />
                    </div>

                    <div className="col-md-4">
                      <label className="form-label mb-1" htmlFor="product-kind">
                        نوع محصول
                      </label>
                      {form.isProductKindLocked ? (
                        <span
                          className="d-block"
                          tabIndex={0}
                          {...tipProps(
                            'پس از استفاده در خرید، فروش، تولید یا موجودی، نوع محصول قفل می‌شود',
                          )}
                        >
                          <select
                            id="product-kind"
                            className="form-select"
                            value={form.productKind}
                            required
                            disabled
                            {...kindValidity}
                          >
                            {PRODUCT_KIND_OPTIONS.map((option) => (
                              <option key={option.value} value={option.value}>
                                {option.label}
                              </option>
                            ))}
                          </select>
                        </span>
                      ) : (
                        <select
                          id="product-kind"
                          className="form-select"
                          value={form.productKind}
                          required
                          {...tipProps(
                            'پس از استفاده در خرید، فروش، تولید یا موجودی، نوع محصول قفل می‌شود',
                          )}
                          {...kindValidity}
                          onChange={(e) => {
                            e.target.setCustomValidity('')
                            setForm({ ...form, productKind: Number(e.target.value) })
                          }}
                        >
                          {PRODUCT_KIND_OPTIONS.map((option) => (
                            <option key={option.value} value={option.value}>
                              {option.label}
                            </option>
                          ))}
                        </select>
                      )}
                    </div>
                    <div className="col-md-4">
                      <label className="form-label mb-1" htmlFor="product-base-unit">
                        واحد پایه
                      </label>
                      {editRow ? (
                        <span
                          className="d-block"
                          tabIndex={0}
                          {...tipProps('واحد پایه پس از ثبت قابل تغییر نیست')}
                        >
                          <select
                            id="product-base-unit"
                            className="form-select"
                            value={form.baseMeaurmentId}
                            required
                            disabled
                            {...baseUnitValidity}
                          >
                            <option value="">انتخاب کنید...</option>
                            {baseUnitOptions.map((option) => (
                              <option key={option.value} value={option.value}>
                                {option.label}
                              </option>
                            ))}
                          </select>
                        </span>
                      ) : (
                        <select
                          id="product-base-unit"
                          className="form-select"
                          value={form.baseMeaurmentId}
                          required
                          {...tipProps('واحد پایه پس از ثبت قابل تغییر نیست')}
                          {...baseUnitValidity}
                          onChange={(e) => {
                            e.target.setCustomValidity('')
                            handleBaseUnitChange(e.target.value)
                          }}
                        >
                          <option value="">انتخاب کنید...</option>
                          {baseUnitOptions.map((option) => (
                            <option key={option.value} value={option.value}>
                              {option.label}
                            </option>
                          ))}
                        </select>
                      )}
                    </div>
                    <div className="col-md-4">
                      <label className="form-label mb-1" htmlFor="product-category">
                        دسته‌بندی
                      </label>
                      <select
                        id="product-category"
                        className="form-select"
                        value={form.categoryId}
                        onChange={(e) => setForm({ ...form, categoryId: e.target.value })}
                      >
                        <option value="">انتخاب کنید...</option>
                        {categoryOptions.map((option) => (
                          <option key={option.value} value={option.value}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                    </div>

                    <div className="col-md-5">
                      <label className="form-label mb-1" htmlFor="product-sale-mode">
                        قیمت فروش پیشنهادی
                      </label>
                      <select
                        id="product-sale-mode"
                        className="form-select"
                        value={form.salePriceMode}
                        required
                        {...tipProps(
                          'در حالت متغیر، قیمت فروش از بهای خرید لحظه‌ای به‌علاوه درصد سود محاسبه می‌شود',
                        )}
                        {...saleModeValidity}
                        onChange={(e) => {
                          e.target.setCustomValidity('')
                          setForm({ ...form, salePriceMode: Number(e.target.value) })
                        }}
                      >
                        {PRODUCT_SALE_PRICE_MODE_OPTIONS.map((option) => (
                          <option key={option.value} value={option.value}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div className="col-md-4">
                      {isFixedSalePrice ? (
                        <>
                          <label className="form-label mb-1" htmlFor="product-sale-price">
                            مبلغ ثابت
                          </label>
                          <div className="input-group">
                            <input
                              id="product-sale-price"
                              type="number"
                              step="any"
                              min="0"
                              className="form-control"
                              value={form.defaultSalePrice}
                              required
                              {...salePriceValidity}
                              onChange={(e) => {
                                e.target.setCustomValidity('')
                                setForm({ ...form, defaultSalePrice: e.target.value })
                              }}
                            />
                            <span className="input-group-text">{baseCurrencySymbol || '—'}</span>
                          </div>
                        </>
                      ) : (
                        <>
                          <label className="form-label mb-1" htmlFor="product-profit-percent">
                            درصد سود
                          </label>
                          <div className="input-group">
                            <input
                              id="product-profit-percent"
                              type="number"
                              step="any"
                              min="0.0001"
                              className="form-control"
                              value={form.saleProfitPercent}
                              required
                              {...tipProps(
                                'قیمت فروش = بهای خرید لحظه‌ای × (۱ + درصد سود)',
                              )}
                              {...profitValidity}
                              onChange={(e) => {
                                e.target.setCustomValidity('')
                                setForm({ ...form, saleProfitPercent: e.target.value })
                              }}
                            />
                            <span className="input-group-text">٪</span>
                          </div>
                        </>
                      )}
                    </div>
                    <div className="col-12">
                      <MultiCheckboxGroup
                        label="واحدهای مجاز محصول"
                        tip="حداقل یک واحد از درخت همان واحد پایه انتخاب کنید"
                        options={meaurmentOptions}
                        selected={form.meaurmentIds}
                        disabled={!form.baseMeaurmentId}
                        onChange={(meaurmentIds) => {
                          const previousDefaultId =
                            form.defaultMeaurmentId || form.baseMeaurmentId
                          const nextDefaultId = meaurmentIds.includes(
                            Number(form.defaultMeaurmentId),
                          )
                            ? form.defaultMeaurmentId
                            : (meaurmentIds[0] ?? '')
                          const fromFactor = getMeaurmentFactor(
                            meaurmentOptions,
                            previousDefaultId,
                          )
                          const toFactor = getMeaurmentFactor(
                            meaurmentOptions,
                            nextDefaultId || form.baseMeaurmentId,
                          )
                          setForm({
                            ...form,
                            meaurmentIds,
                            defaultMeaurmentId: nextDefaultId,
                            minStockQuantity: convertMinStockBetweenUnits(
                              form.minStockQuantity,
                              fromFactor,
                              toFactor,
                            ),
                          })
                        }}
                      />
                    </div>

                    <div className={editRow ? 'col-md-5' : 'col-md-6'}>
                      <label className="form-label mb-1" htmlFor="product-default-unit">
                        واحد پیش‌فرض
                      </label>
                      <select
                        id="product-default-unit"
                        className="form-select"
                        value={form.defaultMeaurmentId}
                        onChange={(e) => {
                          const nextId = e.target.value
                          const fromFactor = getMeaurmentFactor(
                            meaurmentOptions,
                            form.defaultMeaurmentId || form.baseMeaurmentId,
                          )
                          const toFactor = getMeaurmentFactor(
                            meaurmentOptions,
                            nextId || form.baseMeaurmentId,
                          )
                          setForm({
                            ...form,
                            defaultMeaurmentId: nextId,
                            minStockQuantity: convertMinStockBetweenUnits(
                              form.minStockQuantity,
                              fromFactor,
                              toFactor,
                            ),
                          })
                        }}
                      >
                        <option value="">انتخاب کنید...</option>
                        {defaultMeaurmentChoices.map((option) => (
                          <option key={option.value} value={option.value}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                    </div>

                    <div className={editRow ? 'col-md-4' : 'col-md-6'}>
                      <label className="form-label mb-1" htmlFor="product-min-stock">
                        حداقل موجودی
                      </label>
                      <div className="input-group">
                        <input
                          id="product-min-stock"
                          type="number"
                          step="any"
                          min="0"
                          className="form-control"
                          value={form.minStockQuantity}
                          disabled={!form.defaultMeaurmentId && !form.baseMeaurmentId}
                          {...tipProps(
                            'به واحد پیش‌فرض وارد شود؛ برای مقایسه با موجودی انبار به واحد پایه تبدیل و ذخیره می‌شود',
                          )}
                          onChange={(e) =>
                            setForm({ ...form, minStockQuantity: e.target.value })
                          }
                        />
                        <span className="input-group-text text-truncate" style={{ maxWidth: 110 }}>
                          {selectedDefaultUnitLabel || 'واحد پیش‌فرض'}
                        </span>
                      </div>
                    </div>

                    {editRow && (
                      <div className="col-md-3 d-flex align-items-end">
                        <div className="form-check form-switch mb-2">
                          <input
                            className="form-check-input"
                            type="checkbox"
                            id="product-is-active"
                            checked={form.isActive}
                            onChange={(e) =>
                              setForm({ ...form, isActive: e.target.checked })
                            }
                          />
                          <label className="form-check-label" htmlFor="product-is-active">
                            فعال
                          </label>
                        </div>
                      </div>
                    )}
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
                    {submitting ? 'در حال ذخیره...' : 'ذخیره'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </>
      )}

      {deleteRow && (
        <>
          <div className="modal-backdrop show users-modal-backdrop" onClick={closeModals} />
          <div className="modal show d-block users-modal" tabIndex="-1" role="dialog">
            <div className="modal-dialog modal-dialog-centered">
              <div className="modal-content">
                <div className="modal-header">
                  <h5 className="modal-title">حذف محصول</h5>
                  <button
                    type="button"
                    className="btn-close"
                    aria-label="بستن"
                    onClick={closeModals}
                  />
                </div>
                <div className="modal-body">
                  <p className="mb-0">
                    آیا از حذف <strong>{deleteRow.name}</strong> اطمینان دارید؟
                  </p>
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
                    type="button"
                    className="btn btn-danger"
                    onClick={handleDeleteConfirm}
                    disabled={submitting}
                  >
                    {submitting ? 'در حال حذف...' : 'حذف'}
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

export default ProductListPage
