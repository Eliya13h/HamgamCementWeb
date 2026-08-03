import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import Icon from '../../components/common/Icon'
import {
  useModalKeyboardShortcuts,
  usePageCreateShortcut,
  useModalAutoFocus,
} from '../../hooks/useModalKeyboardShortcuts'
import { showAppToast } from '../../lib/appToast'
import DataTable from '../../lib/dataTableSetup'
import { createServerSideTableOptions } from '../../lib/dataTableOptions'
import { persianValidity, validateFormPersian } from '../../lib/persianFormValidity'
import { usePageCrud } from '../../permissions/usePageCrud'
import {
  createEmployee,
  createEmployeesDataTableAjax,
  deleteEmployee,
  fetchDepartments,
  updateEmployee,
} from '../../services/employeesApi'

const TITLE_OPTIONS = [
  { value: 0, label: 'آقا' },
  { value: 1, label: 'خانم' },
]

function EmployeeFormFields({ form, setForm, idPrefix, departments }) {
  return (
    <div className="row g-3">
      <div className="col-md-4">
        <label className="form-label mb-1">عنوان</label>
        <select
          className="form-select"
          value={form.title}
          onChange={(e) => setForm((prev) => ({ ...prev, title: e.target.value }))}
        >
          {TITLE_OPTIONS.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      </div>
      <div className="col-md-4">
        <label className="form-label mb-1">نام</label>
        <input
          type="text"
          className="form-control"
          value={form.name}
          required
          {...persianValidity('لطفاً نام را وارد کنید.')}
          onChange={(e) => {
            e.target.setCustomValidity('')
            setForm((prev) => ({ ...prev, name: e.target.value }))
          }}
        />
      </div>
      <div className="col-md-4">
        <label className="form-label mb-1">نام خانوادگی</label>
        <input
          type="text"
          className="form-control"
          value={form.family}
          required
          {...persianValidity('لطفاً نام خانوادگی را وارد کنید.')}
          onChange={(e) => {
            e.target.setCustomValidity('')
            setForm((prev) => ({ ...prev, family: e.target.value }))
          }}
        />
      </div>
      <div className="col-md-6">
        <label className="form-label mb-1">نام پدر</label>
        <input
          type="text"
          className="form-control"
          value={form.fatherName}
          onChange={(e) => setForm((prev) => ({ ...prev, fatherName: e.target.value }))}
        />
      </div>
      <div className="col-md-6">
        <label className="form-label mb-1">شماره تذکره</label>
        <input
          type="text"
          className="form-control"
          value={form.nationalCode}
          onChange={(e) => setForm((prev) => ({ ...prev, nationalCode: e.target.value }))}
        />
      </div>
      <div className="col-md-6">
        <label className="form-label mb-1">موبایل</label>
        <input
          type="text"
          className="form-control"
          value={form.mobile}
          required
          {...persianValidity('لطفاً موبایل را وارد کنید.')}
          onChange={(e) => {
            e.target.setCustomValidity('')
            setForm((prev) => ({ ...prev, mobile: e.target.value }))
          }}
        />
      </div>
      <div className="col-md-6">
        <label className="form-label mb-1">بخش</label>
        <select
          className="form-select"
          value={form.departmentId}
          required
          {...persianValidity('لطفاً بخش را انتخاب کنید.')}
          onChange={(e) => {
            e.target.setCustomValidity('')
            setForm((prev) => ({ ...prev, departmentId: e.target.value }))
          }}
        >
          <option value="">انتخاب بخش</option>
          {departments.map((department) => (
            <option key={department.departmentId} value={department.departmentId}>
              {department.name}
            </option>
          ))}
        </select>
      </div>
      <div className="col-12">
        <label className="form-label mb-1">آدرس</label>
        <input
          type="text"
          className="form-control"
          value={form.address}
          onChange={(e) => setForm((prev) => ({ ...prev, address: e.target.value }))}
        />
      </div>
      <div className="col-md-6">
        <label className="form-label mb-1">حقوق</label>
        <input
          type="number"
          step="0.0001"
          className="form-control"
          value={form.sallary}
          onChange={(e) => setForm((prev) => ({ ...prev, sallary: e.target.value }))}
        />
      </div>
      <div className="col-12">
        <div className="form-check form-switch">
          <input
            className="form-check-input"
            type="checkbox"
            id={`${idPrefix}-is-active`}
            checked={form.isActive}
            onChange={(e) => setForm((prev) => ({ ...prev, isActive: e.target.checked }))}
          />
          <label className="form-check-label" htmlFor={`${idPrefix}-is-active`}>
            فعال
          </label>
        </div>
      </div>
    </div>
  )
}

function EmployeesPage({ embedded = false }) {
  const tableRef = useRef(null)
  const createFormRef = useRef(null)
  const editFormRef = useRef(null)
  const { canCreate, canEdit, canDelete } = usePageCrud('/people/employees')
  const [departments, setDepartments] = useState([])
  const [loadError, setLoadError] = useState('')
  const [showCreate, setShowCreate] = useState(false)
  const [editRow, setEditRow] = useState(null)
  const [deleteRow, setDeleteRow] = useState(null)
  const [formError, setFormError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [createForm, setCreateForm] = useState({
    title: 0,
    name: '',
    fatherName: '',
    family: '',
    nationalCode: '',
    mobile: '',
    address: '',
    sallary: 0,
    departmentId: '',
    isActive: true,
  })
  const [editForm, setEditForm] = useState({
    title: 0,
    name: '',
    fatherName: '',
    family: '',
    nationalCode: '',
    mobile: '',
    address: '',
    sallary: 0,
    departmentId: '',
    isActive: true,
  })

  useEffect(() => {
    fetchDepartments()
      .then(setDepartments)
      .catch(() => setDepartments([]))
  }, [])

  const reloadTable = useCallback(() => {
    tableRef.current?.dt()?.ajax.reload(null, false)
  }, [])

  const openCreate = useCallback(() => {
    setFormError('')
    setShowCreate(true)
    setCreateForm({
      title: 0,
      name: '',
      fatherName: '',
      family: '',
      nationalCode: '',
      mobile: '',
      address: '',
      sallary: 0,
      departmentId: '',
      isActive: true,
    })
  }, [])

  const openEdit = useCallback((row) => {
    setFormError('')
    setEditRow(row)
    setEditForm({
      title: row.title ?? 0,
      name: row.name,
      fatherName: row.fatherName ?? '',
      family: row.family,
      nationalCode: row.nationalCode ?? '',
      mobile: row.mobile,
      address: row.address ?? '',
      sallary: row.sallary ?? 0,
      departmentId: String(row.departmentId),
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

  const triggerCreateSave = useCallback(() => {
    if (!submitting) createFormRef.current?.requestSubmit()
  }, [submitting])

  const triggerEditSave = useCallback(() => {
    if (!submitting) editFormRef.current?.requestSubmit()
  }, [submitting])

  useModalKeyboardShortcuts({
    open: showCreate,
    onClose: closeModals,
    onSave: triggerCreateSave,
    formRef: createFormRef,
  })
  useModalKeyboardShortcuts({
    open: Boolean(editRow),
    onClose: closeModals,
    onSave: triggerEditSave,
    formRef: editFormRef,
  })
  useModalKeyboardShortcuts({ open: Boolean(deleteRow), onClose: closeModals })
  usePageCreateShortcut({
    enabled: canCreate,
    onNew: openCreate,
    isBlocked: showCreate || Boolean(editRow) || Boolean(deleteRow),
  })
  useModalAutoFocus({ open: showCreate, formRef: createFormRef })
  useModalAutoFocus({ open: Boolean(editRow), formRef: editFormRef })

  const handleCreateSubmit = async (event) => {
    event.preventDefault()
    const formEl = event.currentTarget
    const message = validateFormPersian(formEl)
    if (message) {
      showAppToast(message)
      formEl.reportValidity()
      return
    }

    setSubmitting(true)
    setFormError('')

    try {
      await createEmployee({
        title: Number(createForm.title),
        name: createForm.name,
        fatherName: createForm.fatherName,
        family: createForm.family,
        nationalCode: createForm.nationalCode,
        mobile: createForm.mobile,
        address: createForm.address,
        sallary: Number(createForm.sallary),
        departmentId: Number(createForm.departmentId),
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
    const formEl = event.currentTarget
    const message = validateFormPersian(formEl)
    if (message) {
      showAppToast(message)
      formEl.reportValidity()
      return
    }
    if (!editRow) return

    setSubmitting(true)
    setFormError('')

    try {
      await updateEmployee(editRow.employeeId, {
        title: Number(editForm.title),
        name: editForm.name,
        fatherName: editForm.fatherName,
        family: editForm.family,
        nationalCode: editForm.nationalCode,
        mobile: editForm.mobile,
        address: editForm.address,
        sallary: Number(editForm.sallary),
        departmentId: Number(editForm.departmentId),
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
      await deleteEmployee(deleteRow.employeeId)
      closeModals()
      reloadTable()
    } catch (error) {
      setFormError(error.message)
      setSubmitting(false)
    }
  }

  const tableOptions = useMemo(
    () =>
      createServerSideTableOptions({
        ajax: createEmployeesDataTableAjax(setLoadError),
        order: [[1, 'asc']],
        columns: [
          { data: 'rowNumber', name: 'rowNumber' },
          { data: 'fullName', name: 'fullName' },
          { data: 'nationalCode', name: 'nationalCode' },
          { data: 'mobile', name: 'mobile' },
          { data: 'departmentName', name: 'departmentName' },
          { data: 'sallary', name: 'sallary' },
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
              onClick={() => openDelete(row)}
            >
              <Icon name="trash" />
            </button>
          )}
        </div>
      ),
    }),
    [openEdit, openDelete, canEdit, canDelete],
  )

  const content = (
    <>
      <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between gap-3 flex-wrap">
        <h2 className="card-title mb-0">کارمندان</h2>
        {canCreate && (
          <button
            type="button"
            className="btn btn-sm btn-accent btn-users-new d-inline-flex align-items-center gap-2"
            title="کارمند جدید (Ctrl+N)"
            onClick={openCreate}
          >
            <Icon name="plus" />
            <span>کارمند جدید</span>
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
                <th>نام</th>
                <th>شماره تذکره</th>
                <th>موبایل</th>
                <th>بخش</th>
                <th>حقوق</th>
                <th>وضعیت</th>
                <th>عملیات</th>
              </tr>
            </thead>
          </DataTable>
        </div>
      </div>
    </>
  )

  return (
    <div className="users-page">
      {embedded ? content : <div className="content-card card border-0 h-100">{content}</div>}

      {showCreate && (
        <>
          <div className="modal-backdrop show users-modal-backdrop" onClick={closeModals} />
          <div className="modal show d-block users-modal" tabIndex="-1" role="dialog">
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable modal-lg">
              <form ref={createFormRef} className="modal-content" onSubmit={handleCreateSubmit} noValidate>
                <div className="modal-header">
                  <h5 className="modal-title">کارمند جدید</h5>
                  <button type="button" className="btn-close" aria-label="بستن" onClick={closeModals} />
                </div>
                <div className="modal-body">
                  {formError && <div className="alert alert-danger py-2">{formError}</div>}
                  <EmployeeFormFields
                    form={createForm}
                    setForm={setCreateForm}
                    idPrefix="create-employee"
                    departments={departments}
                  />
                </div>
                <div className="modal-footer">
                  <button type="button" className="btn btn-outline-secondary" onClick={closeModals}>انصراف</button>
                  <button type="submit" className="btn btn-accent" disabled={submitting}>
                    {submitting ? 'در حال ایجاد...' : 'ایجاد کارمند'}
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
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable modal-lg">
              <form ref={editFormRef} className="modal-content" onSubmit={handleEditSubmit} noValidate>
                <div className="modal-header">
                  <h5 className="modal-title">ویرایش کارمند</h5>
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
                  <EmployeeFormFields
                    form={editForm}
                    setForm={setEditForm}
                    idPrefix="edit-employee"
                    departments={departments}
                  />
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
                  <h5 className="modal-title">حذف کارمند</h5>
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
                    آیا از حذف کارمند <strong>{deleteRow.fullName}</strong> اطمینان دارید؟
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

export default EmployeesPage
