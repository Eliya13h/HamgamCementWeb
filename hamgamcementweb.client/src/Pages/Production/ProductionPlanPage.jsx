import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import Icon from '../../components/common/Icon'
import JalaliDateField from '../../components/common/JalaliDateField'
import SearchableSelect from '../../components/common/SearchableSelect'
import DataTable from '../../lib/dataTableSetup'
import { showAppToast } from '../../lib/appToast'
import { persianValidity, validateFormPersian } from '../../lib/persianFormValidity'
import {
  useModalKeyboardShortcuts,
  usePageCreateShortcut,
} from '../../hooks/useModalKeyboardShortcuts'
import { usePageCrud } from '../../permissions/usePageCrud'
import { todayGregorianIso } from '../../lib/afghanSolarCalendar'
import { fetchMeaurmentOptions, fetchProductOptions } from '../../services/productsApi'
import { buildProductionPlanPayload, productionPlansApi } from '../../services/productionApi'
import { dataTableLanguage, formatAmount, formatJalaliDate } from '../Transport/CrudTablePage'

const planColumns = [
  { data: 'productName', title: 'محصول' },
  { data: 'planDate', title: 'تاریخ' },
  { data: 'plannedQuantity', title: 'مقدار برنامه' },
  { data: 'statusLabel', title: 'وضعیت' },
  { data: 'notes', title: 'یادداشت' },
]

function ProductionPlanPage() {
  const tableRef = useRef(null)
  const formRef = useRef(null)
  const navigate = useNavigate()
  const { canCreate, canEdit, canDelete } = usePageCrud('/production/plan')
  const [loadError, setLoadError] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editId, setEditId] = useState(null)
  const [deleteRow, setDeleteRow] = useState(null)
  const [formError, setFormError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [products, setProducts] = useState([])
  const [meaurments, setMeaurments] = useState([])
  const [form, setForm] = useState({
    planDate: '',
    productId: '',
    meaurmentId: '',
    plannedQuantity: '',
    notes: '',
  })

  useEffect(() => {
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
    setDeleteRow(null)
    setFormError('')
    setSubmitting(false)
  }, [])

  const openCreate = useCallback(() => {
    setForm({
      planDate: todayGregorianIso(),
      productId: '',
      meaurmentId: '',
      plannedQuantity: '',
      notes: '',
    })
    setEditId(null)
    setFormError('')
    setShowForm(true)
  }, [])

  const openEdit = useCallback((row) => {
    setForm({
      planDate: String(row.planDate).slice(0, 10),
      productId: row.productId,
      meaurmentId: row.meaurmentId,
      plannedQuantity: row.plannedQuantity,
      notes: row.notes ?? '',
    })
    setEditId(row.productionPlanId)
    setFormError('')
    setShowForm(true)
  }, [])

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
      const payload = buildProductionPlanPayload(form)
      if (editId) {
        await productionPlansApi.update(editId, payload)
      } else {
        await productionPlansApi.create(payload)
      }
      closeModals()
      reloadTable()
      showAppToast(editId ? 'برنامه ویرایش شد.' : 'برنامه ایجاد شد.', 'success')
    } catch (error) {
      setFormError(error.message)
      setSubmitting(false)
      showAppToast(error.message)
    }
  }

  const handleDeleteConfirm = async () => {
    if (!deleteRow) return
    setSubmitting(true)
    setFormError('')
    try {
      await productionPlansApi.remove(deleteRow.productionPlanId)
      closeModals()
      reloadTable()
      showAppToast('برنامه حذف شد.', 'success')
    } catch (error) {
      setFormError(error.message)
      setSubmitting(false)
      showAppToast(error.message)
    }
  }

  const tableOptions = useMemo(() => ({
    processing: true,
    serverSide: true,
    ajax: productionPlansApi.createDataTableAjax(setLoadError),
    paging: true,
    searching: true,
    ordering: true,
    info: true,
    scrollX: true,
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
      { data: 'productName', name: 'productName', title: 'محصول' },
      {
        data: 'planDate',
        name: 'planDate',
        title: 'تاریخ',
        render: (data) => formatJalaliDate(data),
      },
      {
        data: 'plannedQuantity',
        name: 'plannedQuantity',
        title: 'مقدار برنامه',
        render: (data, _type, row) => `${formatAmount(data)} ${row.meaurmentName ?? ''}`.trim(),
      },
      {
        data: 'statusLabel',
        name: 'statusLabel',
        title: 'وضعیت',
        orderable: false,
        render: (_data, _type, row) => {
          if (row.postedBatchesCount > 0) return '<span class="badge badge-active">تولید شده</span>'
          if (row.linkedBatchesCount > 0) return '<span class="badge badge-inactive">در حال تولید</span>'
          return '<span class="badge badge-settled">برنامه‌ریزی</span>'
        },
      },
      { data: 'notes', name: 'notes', title: 'یادداشت', defaultContent: '—' },
      { data: null, name: 'actions', defaultContent: '', title: 'عملیات' },
    ],
    columnDefs: [
      { targets: 0, orderable: false, searchable: false, width: '56px', className: 'text-center' },
      { targets: 6, orderable: false, searchable: false, className: 'text-center all dt-actions-col', width: '160px' },
    ],
  }), [])

  const actionSlots = useMemo(() => ({
    6: (_data, _type, row) => (
      <div className="dt-actions">
        {canCreate && (
          <button
            type="button"
            className="dt-action-btn"
            title="ایجاد سند تولید از این برنامه"
            onClick={() => navigate(`/production/daily?planId=${row.productionPlanId}`)}
          >
            <Icon name="plus" />
          </button>
        )}
        {canEdit && (
          <button type="button" className="dt-action-btn" title="ویرایش" onClick={() => openEdit(row)}>
            <Icon name="edit" />
          </button>
        )}
        {canDelete && (
          <button type="button" className="dt-action-btn btn-delete" title="حذف" onClick={() => setDeleteRow(row)}>
            <Icon name="trash" />
          </button>
        )}
      </div>
    ),
  }), [canCreate, canDelete, canEdit, navigate, openEdit])

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

  return (
    <div className="content-card card border-0 production-page">
      <div className="card-body p-4">
        <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
          <div>
            <h2 className="card-title mb-1">برنامه تولید</h2>
            <p className="text-muted mb-0 small">هدف تولید را مشخص کنید؛ سپس از روی آن سند تولید روزانه بسازید</p>
          </div>
          {canCreate && (
            <button
              type="button"
              className="btn btn-primary d-inline-flex align-items-center gap-2"
              onClick={openCreate}
              title="برنامه جدید (Ctrl+Space)"
            >
              <Icon name="plus" />
              <span>برنامه جدید</span>
            </button>
          )}
        </div>
        {loadError && <div className="alert alert-danger">{loadError}</div>}
        <div className="users-table-wrapper">
          <DataTable ref={tableRef} className="table table-hover w-100 align-middle" options={tableOptions} slots={actionSlots}>
            <thead>
              <tr>
                <th>#</th>
                {planColumns.map((col) => (
                  <th key={col.data}>{col.title}</th>
                ))}
                <th>عملیات</th>
              </tr>
            </thead>
          </DataTable>
        </div>

        {showForm && (
          <div className="modal show d-block production-modal" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
            <div className="modal-dialog modal-dialog-scrollable">
              <div className="modal-content">
                <form ref={formRef} onSubmit={handleSubmit} noValidate>
                  <div className="modal-header">
                    <h5 className="modal-title">{editId ? 'ویرایش برنامه' : 'برنامه تولید جدید'}</h5>
                    <button type="button" className="btn-close" onClick={closeModals} />
                  </div>
                  <div className="modal-body">
                    {formError && <div className="alert alert-danger">{formError}</div>}
                    <div className="row g-3">
                      <div className="col-12">
                        <label className="form-label">تاریخ</label>
                        <JalaliDateField
                          value={form.planDate}
                          onChange={(value) => setForm((prev) => ({ ...prev, planDate: value }))}
                          required
                          requiredMessage="لطفاً تاریخ برنامه را انتخاب کنید."
                        />
                      </div>
                      <div className="col-12">
                        <label className="form-label">محصول</label>
                        <SearchableSelect
                          options={products}
                          value={form.productId}
                          onChange={(value) => setForm((prev) => ({ ...prev, productId: value, meaurmentId: '' }))}
                          required
                          requiredMessage="لطفاً محصول را انتخاب کنید."
                        />
                      </div>
                      <div className="col-md-6">
                        <label className="form-label">واحد</label>
                        <select
                          className="form-select"
                          value={form.meaurmentId}
                          required
                          onChange={(e) => setForm((prev) => ({ ...prev, meaurmentId: e.target.value }))}
                          {...persianValidity('لطفاً واحد را انتخاب کنید.')}
                        >
                          <option value="">—</option>
                          {meaurmentsForProduct(form.productId).map((m) => (
                            <option key={m.value} value={m.value}>{m.label}</option>
                          ))}
                        </select>
                      </div>
                      <div className="col-md-6">
                        <label className="form-label">مقدار برنامه</label>
                        <input
                          type="number"
                          className="form-control"
                          value={form.plannedQuantity}
                          min="0"
                          step="any"
                          required
                          onChange={(e) => setForm((prev) => ({ ...prev, plannedQuantity: e.target.value }))}
                          {...persianValidity('لطفاً مقدار برنامه معتبر وارد کنید.')}
                        />
                      </div>
                      <div className="col-12">
                        <label className="form-label">یادداشت</label>
                        <input
                          type="text"
                          className="form-control"
                          value={form.notes}
                          onChange={(e) => setForm((prev) => ({ ...prev, notes: e.target.value }))}
                        />
                      </div>
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
            <div className="modal-dialog">
              <div className="modal-content">
                <div className="modal-header">
                  <h5 className="modal-title">حذف برنامه</h5>
                  <button type="button" className="btn-close" onClick={closeModals} />
                </div>
                <div className="modal-body">
                  {formError && <div className="alert alert-danger">{formError}</div>}
                  <p>برنامه «{deleteRow.productName}» حذف شود؟</p>
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

export default ProductionPlanPage
