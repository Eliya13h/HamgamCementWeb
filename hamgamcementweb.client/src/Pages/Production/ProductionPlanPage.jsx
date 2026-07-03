import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import Icon from '../../components/common/Icon'
import JalaliDateField from '../../components/common/JalaliDateField'
import SearchableSelect from '../../components/common/SearchableSelect'
import DataTable from '../../lib/dataTableSetup'
import { usePageCrud } from '../../permissions/usePageCrud'
import { todayGregorianIso } from '../../lib/afghanSolarCalendar'
import { fetchMeaurmentOptions, fetchProductOptions } from '../../services/productsApi'
import { buildProductionPlanPayload, productionPlansApi } from '../../services/productionApi'
import { dataTableLanguage, formatAmount, formatJalaliDate } from '../Transport/CrudTablePage'

function ProductionPlanPage() {
  const tableRef = useRef(null)
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
      await productionPlansApi.remove(deleteRow.productionPlanId)
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
        { data: 'productName', name: 'productName' },
        {
          data: 'planDate',
          name: 'planDate',
          render: (data) => formatJalaliDate(data),
        },
        {
          data: 'plannedQuantity',
          name: 'plannedQuantity',
          render: (data, _type, row) => `${formatAmount(data)} ${row.meaurmentName ?? ''}`.trim(),
        },
        { data: 'notes', name: 'notes', orderable: false },
        { data: null, name: 'actions', defaultContent: '' },
      ],
      columnDefs: [
        { targets: 0, orderable: false, searchable: false, width: '56px', className: 'text-center' },
        {
          targets: 5,
          orderable: false,
          searchable: false,
          className: 'text-center all dt-actions-col',
          width: '100px',
        },
      ],
    }),
    [],
  )

  const actionSlots = useMemo(
    () => ({
      5: (_data, _type, row) => (
        <div className="dt-actions">
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
    }),
    [canDelete, canEdit, openEdit],
  )

  return (
    <div className="content-card card border-0">
      <div className="card-body p-4">
        <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
          <div>
            <h2 className="card-title mb-1">برنامه تولید</h2>
            <p className="text-muted mb-0 small">برنامه‌ریزی مقدار تولید محصولات</p>
          </div>
          {canCreate && (
            <button type="button" className="btn btn-primary d-inline-flex align-items-center gap-2" onClick={openCreate}>
              <Icon name="plus" />
              <span>برنامه جدید</span>
            </button>
          )}
        </div>

        {loadError && <div className="alert alert-danger">{loadError}</div>}

        <DataTable ref={tableRef} options={tableOptions} actionSlots={actionSlots} />

        {showForm && (
          <div className="modal show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
            <div className="modal-dialog">
              <div className="modal-content">
                <form onSubmit={handleSubmit}>
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
                        />
                      </div>
                      <div className="col-12">
                        <label className="form-label">محصول</label>
                        <SearchableSelect
                          options={products}
                          value={form.productId}
                          onChange={(value) => setForm((prev) => ({ ...prev, productId: value }))}
                          required
                        />
                      </div>
                      <div className="col-md-6">
                        <label className="form-label">واحد</label>
                        <select
                          className="form-select"
                          value={form.meaurmentId}
                          required
                          onChange={(e) => setForm((prev) => ({ ...prev, meaurmentId: e.target.value }))}
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
                  <p>آیا از حذف برنامه «{deleteRow.productName}» مطمئن هستید؟</p>
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
