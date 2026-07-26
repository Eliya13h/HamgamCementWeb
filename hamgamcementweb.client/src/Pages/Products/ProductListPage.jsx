import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import Icon from '../../components/common/Icon'
import DataTable from '../../lib/dataTableSetup'
import { useModalKeyboardShortcuts } from '../../hooks/useModalKeyboardShortcuts'
import { usePageCrud } from '../../permissions/usePageCrud'
import { fetchBaseCurrency } from '../../services/currenciesApi'
import {
  fetchCategoryOptions,
  fetchBaseMeaurmentOptions,
  fetchMeaurmentOptions,
  fetchNextProductCodePreview,
  productsApi,
} from '../../services/productsApi'
import { dataTableLanguage, formatAmount } from '../Transport/CrudTablePage'

const columns = [
  { data: 'code', title: 'کد' },
  { data: 'name', title: 'نام' },
  { data: 'categoriesText', title: 'دسته‌بندی', orderable: false },
  {
    data: 'suggestedPurchasePrice',
    title: 'بهای خرید (لحظه‌ای)',
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
    render: (data, type) => (type === 'display' ? formatAmount(data) : data),
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
  baseMeaurmentId: '',
  defaultMeaurmentId: '',
  defaultSalePrice: '',
  minStockQuantity: '',
  categoryId: '',
  meaurmentIds: [],
  isActive: true,
}

function MultiCheckboxGroup({ label, options, selected, onChange, disabled }) {
  return (
    <div>
      <label className="form-label">{label}</label>
      <div className="d-flex flex-wrap gap-3 border rounded p-3">
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
                id={`chk-${label}-${option.value}`}
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
              <label
                className="form-check-label"
                htmlFor={`chk-${label}-${option.value}`}
              >
                {option.label}
              </label>
            </div>
          ))
        )}
      </div>
    </div>
  )
}

function ProductListPage() {
  const { canCreate, canEdit, canDelete } = usePageCrud('/products/list')
  const tableRef = useRef(null)
  const formRef = useRef(null)
  const [loadError, setLoadError] = useState('')
  const [showCreate, setShowCreate] = useState(false)
  const [editRow, setEditRow] = useState(null)
  const [deleteRow, setDeleteRow] = useState(null)
  const [formError, setFormError] = useState('')
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
      } catch {
        // ignore
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
    loadMeaurmentsForBase(form.baseMeaurmentId).catch(() => {})
  }, [form.baseMeaurmentId, loadMeaurmentsForBase])

  const defaultMeaurmentChoices = useMemo(
    () => meaurmentOptions.filter((m) => form.meaurmentIds.includes(m.value)),
    [meaurmentOptions, form.meaurmentIds],
  )

  const selectedBaseUnitLabel = useMemo(
    () =>
      baseUnitOptions.find((option) => option.value === Number(form.baseMeaurmentId))
        ?.label ?? '',
    [baseUnitOptions, form.baseMeaurmentId],
  )

  const reloadTable = useCallback(() => {
    tableRef.current?.dt()?.ajax.reload(null, false)
  }, [])

  const openCreate = async () => {
    setFormError('')
    try {
      const preview = await fetchNextProductCodePreview()
      setForm({ ...emptyForm, codePreview: preview?.code ?? '' })
      setShowCreate(true)
    } catch (error) {
      setFormError(error.message)
    }
  }

  const openEdit = async (row) => {
    setFormError('')
    try {
      const detail = await productsApi.getById(row.productId)
      await loadMeaurmentsForBase(detail.baseMeaurmentId)
      setForm({
        codePreview: detail.code ?? '',
        name: detail.name ?? '',
        description: detail.description ?? '',
        baseMeaurmentId: detail.baseMeaurmentId ?? '',
        defaultMeaurmentId: detail.defaultMeaurmentId ?? '',
        defaultSalePrice: detail.defaultSalePrice ?? '',
        minStockQuantity: detail.minStockQuantity ?? '',
        categoryId: detail.categoryIds?.[0] ?? '',
        meaurmentIds: detail.meaurmentIds ?? [],
        isActive: detail.isActive ?? true,
      })
      setEditRow(row)
    } catch (error) {
      setLoadError(error.message)
    }
  }

  const closeModals = useCallback(() => {
    setShowCreate(false)
    setEditRow(null)
    setDeleteRow(null)
    setFormError('')
    setSubmitting(false)
  }, [])

  const modalOpen = showCreate || editRow || deleteRow
  const canSaveForm = showCreate || editRow

  const handleSubmit = useCallback(async (event) => {
    event.preventDefault()
    setSubmitting(true)
    setFormError('')

    const payload = {
      name: form.name.trim(),
      description: form.description.trim() || null,
      baseMeaurmentId: Number(form.baseMeaurmentId),
      defaultMeaurmentId: form.defaultMeaurmentId
        ? Number(form.defaultMeaurmentId)
        : null,
      defaultSalePrice: form.defaultSalePrice === '' ? 0 : Number(form.defaultSalePrice),
      minStockQuantity: form.minStockQuantity === '' ? 0 : Number(form.minStockQuantity),
      categoryIds: form.categoryId ? [Number(form.categoryId)] : [],
      meaurmentIds: form.meaurmentIds,
      isActive: form.isActive,
    }

    try {
      if (editRow) {
        await productsApi.update(editRow.productId, payload)
      } else {
        await productsApi.create(payload)
      }
      closeModals()
      reloadTable()
    } catch (error) {
      setFormError(error.message)
      setSubmitting(false)
    }
  }, [form, editRow, closeModals, reloadTable])

  const triggerSave = useCallback(() => {
    if (!submitting) {
      formRef.current?.requestSubmit()
    }
  }, [submitting])

  useModalKeyboardShortcuts({
    open: modalOpen,
    onClose: closeModals,
    onSave: canSaveForm ? triggerSave : undefined,
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
    setFormError('')
    try {
      await productsApi.remove(deleteRow.productId)
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
      ajax: productsApi.createDataTableAjax(setLoadError),
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
            >
              <Icon name="plus" />
              <span>محصول جدید</span>
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

      {(showCreate || editRow) && (
        <>
          <div className="modal-backdrop show users-modal-backdrop" onClick={closeModals} />
          <div className="modal show d-block users-modal" tabIndex="-1" role="dialog">
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable modal-lg">
              <form className="modal-content" ref={formRef} onSubmit={handleSubmit}>
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
                  {formError && (
                    <div className="alert alert-danger py-2">{formError}</div>
                  )}
                  <div className="row g-3">
                    <div className="col-md-4">
                      <label className="form-label">کد محصول</label>
                      <input
                        className="form-control"
                        value={form.codePreview}
                        readOnly
                        disabled
                      />
                      {!editRow && (
                        <div className="form-text">پس از ذخیره به‌صورت خودکار ثبت می‌شود</div>
                      )}
                    </div>
                    <div className="col-md-8">
                      <label className="form-label">نام محصول</label>
                      <input
                        className="form-control"
                        value={form.name}
                        required
                        onChange={(e) => setForm({ ...form, name: e.target.value })}
                      />
                    </div>
                    <div className="col-12">
                      <label className="form-label">توضیحات</label>
                      <textarea
                        className="form-control"
                        rows={2}
                        value={form.description}
                        onChange={(e) =>
                          setForm({ ...form, description: e.target.value })
                        }
                      />
                    </div>
                    <div className="col-md-6">
                      <label className="form-label">واحد پایه</label>
                      <select
                        className="form-select"
                        value={form.baseMeaurmentId}
                        required
                        disabled={Boolean(editRow)}
                        onChange={(e) => handleBaseUnitChange(e.target.value)}
                      >
                        <option value="">انتخاب کنید...</option>
                        {baseUnitOptions.map((option) => (
                          <option key={option.value} value={option.value}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div className="col-md-3">
                      <label className="form-label">قیمت فروش پیشنهادی</label>
                      <div className="input-group">
                        <input
                          type="number"
                          step="any"
                          className="form-control"
                          value={form.defaultSalePrice}
                          onChange={(e) =>
                            setForm({ ...form, defaultSalePrice: e.target.value })
                          }
                        />
                        <span className="input-group-text">{baseCurrencySymbol || '—'}</span>
                      </div>
                    </div>
                    <div className="col-md-6">
                      <label className="form-label">دسته‌بندی</label>
                      <select
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
                    <div className="col-md-6">
                      <label className="form-label">حداقل موجودی</label>
                      <div className="input-group">
                        <input
                          type="number"
                          step="any"
                          min="0"
                          className="form-control"
                          value={form.minStockQuantity}
                          onChange={(e) =>
                            setForm({ ...form, minStockQuantity: e.target.value })
                          }
                        />
                        <span className="input-group-text">
                          {selectedBaseUnitLabel || 'واحد پایه'}
                        </span>
                      </div>
                      <div className="form-text">
                        در صورت کمتر شدن موجودی کل از این مقدار، در لیست اخطار نمایش داده می‌شود.
                      </div>
                    </div>
                    <div className="col-12">
                      <MultiCheckboxGroup
                        label="واحدهای مجاز محصول"
                        options={meaurmentOptions}
                        selected={form.meaurmentIds}
                        disabled={!form.baseMeaurmentId}
                        onChange={(meaurmentIds) => {
                          const defaultMeaurmentId = meaurmentIds.includes(
                            Number(form.defaultMeaurmentId),
                          )
                            ? form.defaultMeaurmentId
                            : (meaurmentIds[0] ?? '')
                          setForm({ ...form, meaurmentIds, defaultMeaurmentId })
                        }}
                      />
                    </div>
                    <div className="col-md-6">
                      <label className="form-label">واحد پیش‌فرض</label>
                      <select
                        className="form-select"
                        value={form.defaultMeaurmentId}
                        onChange={(e) =>
                          setForm({ ...form, defaultMeaurmentId: e.target.value })
                        }
                      >
                        <option value="">انتخاب کنید...</option>
                        {defaultMeaurmentChoices.map((option) => (
                          <option key={option.value} value={option.value}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div className="col-md-6 d-flex align-items-end">
                      <div className="form-check form-switch">
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
                  {formError && (
                    <div className="alert alert-danger py-2">{formError}</div>
                  )}
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
