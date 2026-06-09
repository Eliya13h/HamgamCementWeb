import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import Icon from '../../components/common/Icon'
import DataTable from '../../lib/dataTableSetup'
import {
  changeUserPassword,
  createUser,
  createUsersDataTableAjax,
  deleteUser,
  fetchAvailableEmployees,
  fetchUserRoles,
  updateUser,
} from '../../services/usersApi'

const TITLE_OPTIONS = [
  { value: 0, label: 'آقا' },
  { value: 1, label: 'خانم' },
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

function UsersPage() {
  const tableRef = useRef(null)
  const [roles, setRoles] = useState([])
  const [employees, setEmployees] = useState([])
  const [loadError, setLoadError] = useState('')
  const [showCreateUser, setShowCreateUser] = useState(false)
  const [editUser, setEditUser] = useState(null)
  const [passwordUser, setPasswordUser] = useState(null)
  const [deleteUserRow, setDeleteUserRow] = useState(null)
  const [formError, setFormError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const [editForm, setEditForm] = useState({
    userName: '',
    fullName: '',
    email: '',
    roleId: '',
    isActive: true,
    title: 0,
  })

  const [passwordForm, setPasswordForm] = useState({
    password: '',
    confirmPassword: '',
  })

  const [createForm, setCreateForm] = useState({
    userName: '',
    fullName: '',
    email: '',
    roleId: '',
    employeeId: '',
    isActive: true,
    title: 0,
    password: '',
    confirmPassword: '',
  })

  useEffect(() => {
    fetchUserRoles()
      .then(setRoles)
      .catch(() => setRoles([]))
  }, [])

  const reloadTable = useCallback(() => {
    tableRef.current?.dt()?.ajax.reload(null, false)
  }, [])

  const openEdit = useCallback((row) => {
    setFormError('')
    setEditUser(row)
    setEditForm({
      userName: row.userName,
      fullName: row.fullName,
      email: row.email,
      roleId: String(row.roleId),
      isActive: row.isActive,
      title: row.title ?? 0,
    })
  }, [])

  const openPassword = useCallback((row) => {
    setFormError('')
    setPasswordUser(row)
    setPasswordForm({ password: '', confirmPassword: '' })
  }, [])

  const openDelete = useCallback((row) => {
    setFormError('')
    setDeleteUserRow(row)
  }, [])

  const openCreate = useCallback(() => {
    setFormError('')
    setShowCreateUser(true)
    setCreateForm({
      userName: '',
      fullName: '',
      email: '',
      roleId: '',
      employeeId: '',
      isActive: true,
      title: 0,
      password: '',
      confirmPassword: '',
    })
    fetchAvailableEmployees()
      .then(setEmployees)
      .catch(() => setEmployees([]))
  }, [])

  const closeModals = () => {
    setShowCreateUser(false)
    setEditUser(null)
    setPasswordUser(null)
    setDeleteUserRow(null)
    setFormError('')
    setSubmitting(false)
  }

  const handleCreateSubmit = async (event) => {
    event.preventDefault()

    if (createForm.password !== createForm.confirmPassword) {
      setFormError('رمز عبور و تکرار آن یکسان نیستند.')
      return
    }

    setSubmitting(true)
    setFormError('')

    try {
      await createUser({
        userName: createForm.userName,
        fullName: createForm.fullName,
        email: createForm.email,
        roleId: Number(createForm.roleId),
        employeeId: Number(createForm.employeeId),
        isActive: createForm.isActive,
        title: Number(createForm.title),
        password: createForm.password,
        confirmPassword: createForm.confirmPassword,
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
    if (!editUser) return

    setSubmitting(true)
    setFormError('')

    try {
      await updateUser(editUser.userId, {
        userName: editForm.userName,
        fullName: editForm.fullName,
        email: editForm.email,
        roleId: Number(editForm.roleId),
        isActive: editForm.isActive,
        title: Number(editForm.title),
      })
      closeModals()
      reloadTable()
    } catch (error) {
      setFormError(error.message)
      setSubmitting(false)
    }
  }

  const handlePasswordSubmit = async (event) => {
    event.preventDefault()
    if (!passwordUser) return

    if (passwordForm.password !== passwordForm.confirmPassword) {
      setFormError('رمز عبور و تکرار آن یکسان نیستند.')
      return
    }

    setSubmitting(true)
    setFormError('')

    try {
      await changeUserPassword(passwordUser.userId, {
        password: passwordForm.password,
        confirmPassword: passwordForm.confirmPassword,
      })
      closeModals()
    } catch (error) {
      setFormError(error.message)
      setSubmitting(false)
    }
  }

  const handleDeleteConfirm = async () => {
    if (!deleteUserRow) return

    setSubmitting(true)
    setFormError('')

    try {
      await deleteUser(deleteUserRow.userId)
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
      ajax: createUsersDataTableAjax(setLoadError),
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
          search: {
            placeholder: 'جستجو...',
          },
          pageLength: {
            menu: [10, 15, 25, 50, 100],
          },
        },
        bottomStart: 'info',
        bottomEnd: {
          paging: {
            firstLast: true,
            previousNext: true,
            numbers: 5,
          },
        },
      },
      columns: [
        { data: 'rowNumber', name: 'rowNumber' },
        { data: 'fullName', name: 'fullName' },
        { data: 'userName', name: 'userName' },
        { data: 'email', name: 'email' },
        { data: 'roleName', name: 'roleName' },
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
        { targets: 5, className: 'text-center' },
        {
          targets: 6,
          orderable: false,
          searchable: false,
          className: 'text-center all dt-actions-col',
          width: '130px',
        },
      ],
    }),
    [],
  )

  const actionSlots = useMemo(
    () => ({
      6: (_data, _type, row) => (
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
            className="dt-action-btn"
            title="تغییر رمز عبور"
            onClick={() => openPassword(row)}
          >
            <Icon name="key" />
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
    [openEdit, openPassword, openDelete],
  )

  return (
    <div className="users-page">
      <div className="content-card card border-0 h-100">
        <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between gap-3 flex-wrap">
          <h2 className="card-title mb-0">مدیریت کاربران</h2>
          <button
            type="button"
            className="btn btn-sm btn-accent btn-users-new d-inline-flex align-items-center gap-2"
            title="کاربر جدید"
            onClick={openCreate}
          >
            <Icon name="plus" />
            <span>کاربر جدید</span>
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
                  <th>نام کاربری</th>
                  <th>ایمیل</th>
                  <th>نقش</th>
                  <th>وضعیت</th>
                  <th>عملیات</th>
                </tr>
              </thead>
            </DataTable>
          </div>
        </div>
      </div>

      {showCreateUser && (
        <>
          <div className="modal-backdrop show users-modal-backdrop" onClick={closeModals} />
          <div className="modal show d-block users-modal" tabIndex="-1" role="dialog">
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable">
              <form className="modal-content" onSubmit={handleCreateSubmit}>
              <div className="modal-header">
                <h5 className="modal-title">کاربر جدید</h5>
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
                  <label className="form-label">کارمند</label>
                  <select
                    className="form-select"
                    value={createForm.employeeId}
                    onChange={(e) =>
                      setCreateForm((prev) => ({ ...prev, employeeId: e.target.value }))
                    }
                    required
                  >
                    <option value="">انتخاب کارمند</option>
                    {employees.map((employee) => (
                      <option key={employee.employeeId} value={employee.employeeId}>
                        {employee.fullName}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="mb-3">
                  <label className="form-label">عنوان</label>
                  <select
                    className="form-select"
                    value={createForm.title}
                    onChange={(e) =>
                      setCreateForm((prev) => ({ ...prev, title: e.target.value }))
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
                  <label className="form-label">نام کامل</label>
                  <input
                    type="text"
                    className="form-control"
                    value={createForm.fullName}
                    onChange={(e) =>
                      setCreateForm((prev) => ({ ...prev, fullName: e.target.value }))
                    }
                    required
                  />
                </div>
                <div className="mb-3">
                  <label className="form-label">نام کاربری</label>
                  <input
                    type="text"
                    className="form-control"
                    value={createForm.userName}
                    onChange={(e) =>
                      setCreateForm((prev) => ({ ...prev, userName: e.target.value }))
                    }
                    required
                  />
                </div>
                <div className="mb-3">
                  <label className="form-label">ایمیل</label>
                  <input
                    type="email"
                    className="form-control"
                    value={createForm.email}
                    onChange={(e) =>
                      setCreateForm((prev) => ({ ...prev, email: e.target.value }))
                    }
                    required
                  />
                </div>
                <div className="mb-3">
                  <label className="form-label">نقش</label>
                  <select
                    className="form-select"
                    value={createForm.roleId}
                    onChange={(e) =>
                      setCreateForm((prev) => ({ ...prev, roleId: e.target.value }))
                    }
                    required
                  >
                    <option value="">انتخاب نقش</option>
                    {roles.map((role) => (
                      <option key={role.roleId} value={role.roleId}>
                        {role.name}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="mb-3">
                  <label className="form-label">رمز عبور</label>
                  <input
                    type="password"
                    className="form-control"
                    value={createForm.password}
                    onChange={(e) =>
                      setCreateForm((prev) => ({ ...prev, password: e.target.value }))
                    }
                    required
                    minLength={4}
                  />
                </div>
                <div className="mb-3">
                  <label className="form-label">تکرار رمز عبور</label>
                  <input
                    type="password"
                    className="form-control"
                    value={createForm.confirmPassword}
                    onChange={(e) =>
                      setCreateForm((prev) => ({
                        ...prev,
                        confirmPassword: e.target.value,
                      }))
                    }
                    required
                    minLength={4}
                  />
                </div>
                <div className="form-check form-switch">
                  <input
                    className="form-check-input"
                    type="checkbox"
                    id="create-is-active"
                    checked={createForm.isActive}
                    onChange={(e) =>
                      setCreateForm((prev) => ({ ...prev, isActive: e.target.checked }))
                    }
                  />
                  <label className="form-check-label" htmlFor="create-is-active">
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
                  {submitting ? 'در حال ایجاد...' : 'ایجاد کاربر'}
                </button>
              </div>
              </form>
            </div>
          </div>
        </>
      )}

      {editUser && (
        <>
          <div className="modal-backdrop show users-modal-backdrop" onClick={closeModals} />
          <div className="modal show d-block users-modal" tabIndex="-1" role="dialog">
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable">
              <form className="modal-content" onSubmit={handleEditSubmit}>
              <div className="modal-header">
                <h5 className="modal-title">ویرایش کاربر</h5>
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
                  <label className="form-label">نام کامل</label>
                  <input
                    type="text"
                    className="form-control"
                    value={editForm.fullName}
                    onChange={(e) =>
                      setEditForm((prev) => ({ ...prev, fullName: e.target.value }))
                    }
                    required
                  />
                </div>
                <div className="mb-3">
                  <label className="form-label">نام کاربری</label>
                  <input
                    type="text"
                    className="form-control"
                    value={editForm.userName}
                    onChange={(e) =>
                      setEditForm((prev) => ({ ...prev, userName: e.target.value }))
                    }
                    required
                  />
                </div>
                <div className="mb-3">
                  <label className="form-label">ایمیل</label>
                  <input
                    type="email"
                    className="form-control"
                    value={editForm.email}
                    onChange={(e) =>
                      setEditForm((prev) => ({ ...prev, email: e.target.value }))
                    }
                    required
                  />
                </div>
                <div className="mb-3">
                  <label className="form-label">نقش</label>
                  <select
                    className="form-select"
                    value={editForm.roleId}
                    onChange={(e) =>
                      setEditForm((prev) => ({ ...prev, roleId: e.target.value }))
                    }
                    required
                  >
                    <option value="">انتخاب نقش</option>
                    {roles.map((role) => (
                      <option key={role.roleId} value={role.roleId}>
                        {role.name}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="form-check form-switch">
                  <input
                    className="form-check-input"
                    type="checkbox"
                    id="edit-is-active"
                    checked={editForm.isActive}
                    onChange={(e) =>
                      setEditForm((prev) => ({ ...prev, isActive: e.target.checked }))
                    }
                  />
                  <label className="form-check-label" htmlFor="edit-is-active">
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

      {passwordUser && (
        <>
          <div className="modal-backdrop show users-modal-backdrop" onClick={closeModals} />
          <div className="modal show d-block users-modal" tabIndex="-1" role="dialog">
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable">
              <form className="modal-content" onSubmit={handlePasswordSubmit}>
              <div className="modal-header">
                <h5 className="modal-title">تغییر رمز عبور</h5>
                <button
                  type="button"
                  className="btn-close"
                  aria-label="بستن"
                  onClick={closeModals}
                />
              </div>
              <div className="modal-body">
                <p className="text-muted small mb-3">
                  کاربر: <strong>{passwordUser.fullName}</strong> ({passwordUser.userName})
                </p>
                {formError && (
                  <div className="alert alert-danger py-2">{formError}</div>
                )}
                <div className="mb-3">
                  <label className="form-label">رمز عبور جدید</label>
                  <input
                    type="password"
                    className="form-control"
                    value={passwordForm.password}
                    onChange={(e) =>
                      setPasswordForm((prev) => ({ ...prev, password: e.target.value }))
                    }
                    required
                    minLength={4}
                  />
                </div>
                <div className="mb-3">
                  <label className="form-label">تکرار رمز عبور</label>
                  <input
                    type="password"
                    className="form-control"
                    value={passwordForm.confirmPassword}
                    onChange={(e) =>
                      setPasswordForm((prev) => ({
                        ...prev,
                        confirmPassword: e.target.value,
                      }))
                    }
                    required
                    minLength={4}
                  />
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
                  {submitting ? 'در حال ذخیره...' : 'تغییر رمز'}
                </button>
              </div>
              </form>
            </div>
          </div>
        </>
      )}

      {deleteUserRow && (
        <>
          <div className="modal-backdrop show users-modal-backdrop" onClick={closeModals} />
          <div className="modal show d-block users-modal" tabIndex="-1" role="dialog">
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable">
              <div className="modal-content">
              <div className="modal-header">
                <h5 className="modal-title">حذف کاربر</h5>
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
                  آیا از حذف کاربر{' '}
                  <strong>{deleteUserRow.fullName}</strong> اطمینان دارید؟
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

export default UsersPage
