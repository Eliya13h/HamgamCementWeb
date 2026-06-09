import { useCallback, useMemo, useRef, useState } from 'react'
import Icon from '../../components/common/Icon'
import DataTable from '../../lib/dataTableSetup'
import {
  createSupplier,
  createSuppliersDataTableAjax,
  deleteSupplier,
  updateSupplier,
} from '../../services/suppliersApi'

const TITLE_OPTIONS = [
  { value: 0, label: 'آقا' },
  { value: 1, label: 'خانم' },
]

const PERSON_TYPE_OPTIONS = [
  { value: 1, label: 'حقیقی' },
  { value: 2, label: 'حقوقی' },
]

const dataTableLanguage = {
  emptyTable: 'داده‌ای برای نمایش وجود ندارد',
  info: 'نمایش _START_ تا _END_ از _TOTAL_ ردیف',
  infoEmpty: 'رکوردی یافت نشد',
  infoFiltered: '(فیلتر شده از _MAX_ ردیف)',
  lengthMenu: 'نمایش _MENU_ ردیف',
  loadingRecords: 'در حال بارگذاری...',
  processing: 'در حال پردازش...',
  search: 'جستجو:',
  zeroRecords: 'رکوردی یافت نشد',
  paginate: {
    first: 'اول',
    last: 'آخر',
    next: 'بعدی',
    previous: 'قبلی',
  },
}

function SuppliersPage() {
  const tableRef = useRef(null)
  const [loadError, setLoadError] = useState('')
  const [showCreate, setShowCreate] = useState(false)
  const [editRow, setEditRow] = useState(null)
  const [deleteRow, setDeleteRow] = useState(null)
  const [formError, setFormError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [createForm, setCreateForm] = useState({
    title: 0,
    name: '',
    phoneNumber: '',
    address: '',
    city: '',
    country: '',
    initialBalance: 0,
    supplierType: 1,
    isActive: true,
  })
  const [editForm, setEditForm] = useState({
    title: 0,
    name: '',
    phoneNumber: '',
    address: '',
    city: '',
    country: '',
    initialBalance: 0,
    supplierType: 1,
    isActive: true,
  })

  const reloadTable = useCallback(() => {
    tableRef.current?.dt()?.ajax.reload(null, false)
  }, [])

  const openCreate = useCallback(() => {
    setFormError('')
    setShowCreate(true)
    setCreateForm({
      title: 0,
      name: '',
      phoneNumber: '',
      address: '',
      city: '',
      country: '',
      initialBalance: 0,
      supplierType: 1,
      isActive: true,
    })
  }, [])

  const openEdit = useCallback((row) => {
    setFormError('')
    setEditRow(row)
    setEditForm({
      title: row.title ?? 0,
      name: row.name,
      phoneNumber: row.phoneNumber,
      address: row.address ?? '',
      city: row.city ?? '',
      country: row.country ?? '',
      initialBalance: row.initialBalance ?? 0,
      supplierType: row.supplierType ?? 1,
      isActive: row.isActive,
    })
  }, [])

  const openDelete = useCallback((row) => {
    setFormError('')
    setDeleteRow(row)
  }, [])

  const closeModals = () => {
    setShowCreate(false)
    setEditRow(null)
    setDeleteRow(null)
    setFormError('')
    setSubmitting(false)
  }

  const handleCreateSubmit = async (event) => {
    event.preventDefault()

    setSubmitting(true)
    setFormError('')

    try {
      await createSupplier({
        title: Number(createForm.title),
        name: createForm.name,
        phoneNumber: createForm.phoneNumber,
        address: createForm.address,
        city: createForm.city,
        country: createForm.country,
        initialBalance: Number(createForm.initialBalance),
        supplierType: Number(createForm.supplierType),
        isActive: createForm.isActive,
      })
      closeModals()
      reloadTable()
    } catch (error) {
      setFormError(error.message)
      setSubmitting(false)
    }
  }

  const handleEditSubmit = async (event) => {
    event.preventDefault()
    if (!editRow) return

    setSubmitting(true)
    setFormError('')

    try {
      await updateSupplier(editRow.supplierId, {
        title: Number(editForm.title),
        name: editForm.name,
        phoneNumber: editForm.phoneNumber,
        address: editForm.address,
        city: editForm.city,
        country: editForm.country,
        initialBalance: Number(editForm.initialBalance),
        supplierType: Number(editForm.supplierType),
        isActive: editForm.isActive,
      })
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
      await deleteSupplier(deleteRow.supplierId)
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
      ajax: createSuppliersDataTableAjax(setLoadError),
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
        bottomStart: 'info',
        bottomEnd: {
          paging: { firstLast: true, previousNext: true, numbers: 5 },
        },
      },
      columns: [
        { data: 'rowNumber', name: 'rowNumber' },
        { data: 'name', name: 'name' },
        { data: 'phoneNumber', name: 'phoneNumber' },
        { data: 'city', name: 'city' },
        { data: 'supplierTypeName', name: 'supplierTypeName' },
        { data: 'initialBalance', name: 'initialBalance' },
        {
          data: 'isActive',
          name: 'isActive',
          render: (data) =>
            data
              ? '<span class="badge badge-active">فعال</span>'
              : '<span class="badge badge-inactive">غیرفعال</span>',
        },
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
        { targets: 6, className: 'text-center' },
        {
          targets: 7,
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
      7: (_data, _type, row) => (
        <div className="dt-actions">
          <button
            type="button"
            className="dt-action-btn"
            title="ویرایش"
            onClick={() => openEdit(row)}
          >
            <Icon name="edit" />
          </button>
          <button
            type="button"
            className="dt-action-btn btn-delete"
            title="حذف"
            onClick={() => openDelete(row)}
          >
            <Icon name="trash" />
          </button>
        </div>
      ),
    }),
    [openEdit, openDelete],
  )

  return (
    <div className="users-page">
      <div className="content-card card border-0 h-100">
        <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between gap-3 flex-wrap">
          <h2 className="card-title mb-0">تأمین‌کنندگان</h2>
          <button
            type="button"
            className="btn btn-sm btn-accent btn-users-new d-inline-flex align-items-center gap-2"
            title="تأمین‌کننده جدید"
            onClick={openCreate}
          >
            <Icon name="plus" />
            <span>تأمین‌کننده جدید</span>
          </button>
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
                  <th>نام</th>
                  <th>تلفن</th>
                  <th>شهر</th>
                  <th>نوع</th>
                  <th>موجودی اولیه</th>
                  <th>وضعیت</th>
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
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable">
              <form className="modal-content" onSubmit={handleCreateSubmit}>
                <div className="modal-header">
                  <h5 className="modal-title">تأمین‌کننده جدید</h5>
                  <button type="button" className="btn-close" aria-label="بستن" onClick={closeModals} />
                </div>
                <div className="modal-body">
                  {formError && <div className="alert alert-danger py-2">{formError}</div>}
                  <div className="mb-3">
                    <label className="form-label">عنوان</label>
                    <select
                      className="form-select"
                      value={createForm.title}
                      onChange={(e) => setCreateForm((prev) => ({ ...prev, title: e.target.value }))}
                    >
                      {TITLE_OPTIONS.map((option) => (
                        <option key={option.value} value={option.value}>{option.label}</option>
                      ))}
                    </select>
                  </div>
                  <div className="mb-3">
                    <label className="form-label">نام</label>
                    <input type="text" className="form-control" value={createForm.name}
                      onChange={(e) => setCreateForm((prev) => ({ ...prev, name: e.target.value }))} required />
                  </div>
                  <div className="mb-3">
                    <label className="form-label">تلفن</label>
                    <input type="text" className="form-control" value={createForm.phoneNumber}
                      onChange={(e) => setCreateForm((prev) => ({ ...prev, phoneNumber: e.target.value }))} required />
                  </div>
                  <div className="mb-3">
                    <label className="form-label">آدرس</label>
                    <input type="text" className="form-control" value={createForm.address}
                      onChange={(e) => setCreateForm((prev) => ({ ...prev, address: e.target.value }))} />
                  </div>
                  <div className="row g-3 mb-3">
                    <div className="col-md-6">
                      <label className="form-label">شهر</label>
                      <input type="text" className="form-control" value={createForm.city}
                        onChange={(e) => setCreateForm((prev) => ({ ...prev, city: e.target.value }))} />
                    </div>
                    <div className="col-md-6">
                      <label className="form-label">کشور</label>
                      <input type="text" className="form-control" value={createForm.country}
                        onChange={(e) => setCreateForm((prev) => ({ ...prev, country: e.target.value }))} />
                    </div>
                  </div>
                  <div className="mb-3">
                    <label className="form-label">نوع</label>
                    <select className="form-select" value={createForm.supplierType}
                      onChange={(e) => setCreateForm((prev) => ({ ...prev, supplierType: e.target.value }))}>
                      {PERSON_TYPE_OPTIONS.map((option) => (
                        <option key={option.value} value={option.value}>{option.label}</option>
                      ))}
                    </select>
                  </div>
                  <div className="mb-3">
                    <label className="form-label">موجودی اولیه</label>
                    <input type="number" step="0.0001" className="form-control" value={createForm.initialBalance}
                      onChange={(e) => setCreateForm((prev) => ({ ...prev, initialBalance: e.target.value }))} />
                  </div>
                  <div className="form-check form-switch">
                    <input className="form-check-input" type="checkbox" id="create-supplier-is-active"
                      checked={createForm.isActive}
                      onChange={(e) => setCreateForm((prev) => ({ ...prev, isActive: e.target.checked }))} />
                    <label className="form-check-label" htmlFor="create-supplier-is-active">فعال</label>
                  </div>
                </div>
                <div className="modal-footer">
                  <button type="button" className="btn btn-outline-secondary" onClick={closeModals}>انصراف</button>
                  <button type="submit" className="btn btn-accent" disabled={submitting}>
                    {submitting ? 'در حال ایجاد...' : 'ایجاد تأمین‌کننده'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </>
      )}

      {editRow && (
        <>
          <div className="modal-backdrop show users-modal-backdrop" onClick={closeModals} />
          <div className="modal show d-block users-modal" tabIndex="-1" role="dialog">
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable">
              <form className="modal-content" onSubmit={handleEditSubmit}>
                <div className="modal-header">
                  <h5 className="modal-title">ویرایش تأمین‌کننده</h5>
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
                  <div className="mb-3">
                    <label className="form-label">عنوان</label>
                    <select
                      className="form-select"
                      value={editForm.title}
                      onChange={(e) =>
                        setEditForm((prev) => ({ ...prev, title: e.target.value }))
                      }
                    >
                      {TITLE_OPTIONS.map((option) => (
                        <option key={option.value} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div className="mb-3">
                    <label className="form-label">نام</label>
                    <input
                      type="text"
                      className="form-control"
                      value={editForm.name}
                      onChange={(e) =>
                        setEditForm((prev) => ({ ...prev, name: e.target.value }))
                      }
                      required
                    />
                  </div>
                  <div className="mb-3">
                    <label className="form-label">تلفن</label>
                    <input
                      type="text"
                      className="form-control"
                      value={editForm.phoneNumber}
                      onChange={(e) =>
                        setEditForm((prev) => ({ ...prev, phoneNumber: e.target.value }))
                      }
                      required
                    />
                  </div>
                  <div className="mb-3">
                    <label className="form-label">آدرس</label>
                    <input
                      type="text"
                      className="form-control"
                      value={editForm.address}
                      onChange={(e) =>
                        setEditForm((prev) => ({ ...prev, address: e.target.value }))
                      }
                    />
                  </div>
                  <div className="row g-3 mb-3">
                    <div className="col-md-6">
                      <label className="form-label">شهر</label>
                      <input
                        type="text"
                        className="form-control"
                        value={editForm.city}
                        onChange={(e) =>
                          setEditForm((prev) => ({ ...prev, city: e.target.value }))
                        }
                      />
                    </div>
                    <div className="col-md-6">
                      <label className="form-label">کشور</label>
                      <input
                        type="text"
                        className="form-control"
                        value={editForm.country}
                        onChange={(e) =>
                          setEditForm((prev) => ({ ...prev, country: e.target.value }))
                        }
                      />
                    </div>
                  </div>
                  <div className="mb-3">
                    <label className="form-label">نوع</label>
                    <select
                      className="form-select"
                      value={editForm.supplierType}
                      onChange={(e) =>
                        setEditForm((prev) => ({ ...prev, supplierType: e.target.value }))
                      }
                    >
                      {PERSON_TYPE_OPTIONS.map((option) => (
                        <option key={option.value} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div className="mb-3">
                    <label className="form-label">موجودی اولیه</label>
                    <input
                      type="number"
                      step="0.0001"
                      className="form-control"
                      value={editForm.initialBalance}
                      onChange={(e) =>
                        setEditForm((prev) => ({
                          ...prev,
                          initialBalance: e.target.value,
                        }))
                      }
                    />
                  </div>
                  <div className="form-check form-switch">
                    <input
                      className="form-check-input"
                      type="checkbox"
                      id="supplier-is-active"
                      checked={editForm.isActive}
                      onChange={(e) =>
                        setEditForm((prev) => ({ ...prev, isActive: e.target.checked }))
                      }
                    />
                    <label className="form-check-label" htmlFor="supplier-is-active">
                      فعال
                    </label>
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
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable">
              <div className="modal-content">
                <div className="modal-header">
                  <h5 className="modal-title">حذف تأمین‌کننده</h5>
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
                    آیا از حذف تأمین‌کننده <strong>{deleteRow.name}</strong> اطمینان دارید؟
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

export default SuppliersPage
