import { useCallback, useMemo, useRef, useState } from 'react'
import Icon from '../../components/common/Icon'
import {
  useModalKeyboardShortcuts,
  useModalAutoFocus,
} from '../../hooks/useModalKeyboardShortcuts'
import { showAppToast } from '../../lib/appToast'
import DataTable from '../../lib/dataTableSetup'
import { validateFormPersian } from '../../lib/persianFormValidity'
import PermissionTree from '../../permissions/PermissionTree'
import { usePageCrud } from '../../permissions/usePageCrud'
import { permissionTree } from '../../permissions/registry'
import {
  createUsersDataTableAjax,
  fetchUserPermissions,
  updateUserPermissions,
} from '../../services/usersApi'

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

const emptyForm = {
  hasFullAccess: true,
  permissions: new Set(),
}

function AccessLevelsPage() {
  const tableRef = useRef(null)
  const editFormRef = useRef(null)
  const { can } = usePageCrud('/users/roles')
  const [loadError, setLoadError] = useState('')
  const [editUser, setEditUser] = useState(null)
  const [formError, setFormError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [form, setForm] = useState(emptyForm)

  const reloadTable = useCallback(() => {
    tableRef.current?.dt()?.ajax.reload(null, false)
  }, [])

  const openEdit = useCallback(async (row) => {
    setFormError('')
    setEditUser(row)
    setSubmitting(true)

    try {
      const user = await fetchUserPermissions(row.userId)
      setForm({
        hasFullAccess: user.hasFullAccess,
        permissions: new Set(user.permissions ?? []),
      })
    } catch (error) {
      setFormError(error.message)
      setEditUser(null)
    } finally {
      setSubmitting(false)
    }
  }, [])

  const closeModal = () => {
    setEditUser(null)
    setFormError('')
    setSubmitting(false)
    setForm({ ...emptyForm, permissions: new Set() })
  }

  const triggerEditSave = useCallback(() => {
    if (!submitting) editFormRef.current?.requestSubmit()
  }, [submitting])

  useModalKeyboardShortcuts({
    open: Boolean(editUser),
    onClose: closeModal,
    onSave: triggerEditSave,
    formRef: editFormRef,
  })
  useModalAutoFocus({ open: Boolean(editUser), formRef: editFormRef })

  const handleSubmit = async (event) => {
    event.preventDefault()
    const formEl = event.currentTarget
    const message = validateFormPersian(formEl)
    if (message) {
      showAppToast(message)
      formEl.reportValidity()
      return
    }
    if (!editUser) return

    setSubmitting(true)
    setFormError('')

    try {
      await updateUserPermissions(editUser.userId, {
        hasFullAccess: form.hasFullAccess,
        permissions: form.hasFullAccess ? [] : Array.from(form.permissions),
      })
      closeModal()
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
      scrollX: false,
      autoWidth: false,
      responsive: true,
      stripeClasses: ['odd', 'even'],
      order: [[1, 'asc']],
      pageLength: 15,
      lengthMenu: [10, 15, 25, 50],
      language: dataTableLanguage,
      layout: {
        topStart: {
          search: { placeholder: 'جستجو...' },
          pageLength: { menu: [10, 15, 25, 50] },
        },
        topEnd: null,

        bottomStart: 'info',
        bottomEnd: {
          paging: { firstLast: true, previousNext: true, numbers: 5 },
        },
      },
      columns: [
        { data: 'rowNumber', name: 'rowNumber' },
        { data: 'fullName', name: 'fullName' },
        { data: 'userName', name: 'userName' },
        { data: 'roleName', name: 'roleName' },
        {
          data: 'hasFullAccess',
          name: 'hasFullAccess',
          className: 'text-center',
          orderable: false,
          render: (data) =>
            data
              ? '<span class="badge badge-active">دسترسی کامل</span>'
              : '<span class="badge badge-inactive">محدود</span>',
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
        { targets: 4, className: 'text-center' },
        {
          targets: 5,
          orderable: false,
          searchable: false,
          className: 'text-center all dt-actions-col',
          width: '80px',
        },
      ],
    }),
    [],
  )

  const actionSlots = useMemo(
    () => ({
      5: (_data, _type, row) =>
        can('manage') ? (
          <div className="dt-actions">
            <button
              type="button"
              className="dt-action-btn"
              title="ویرایش دسترسی‌ها"
              onClick={() => openEdit(row)}
            >
              <Icon name="key" />
            </button>
          </div>
        ) : null,
    }),
    [openEdit, can],
  )

  return (
    <div className="users-page">
      <div className="content-card card border-0 h-100">
        <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0">
          <h2 className="card-title mb-1">سطح دسترسی</h2>
          <p className="text-muted small mb-0">
            تعیین دسترسی هر کاربر به بخش‌های مختلف — نقش کاربر فقط نمادین است
          </p>
        </div>

        <div className="card-body card-body-table">
          {loadError && (
            <div className="alert alert-danger py-2 users-load-error mb-0">{loadError}</div>
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
                  <th>نقش</th>
                  <th>نوع دسترسی</th>
                  <th>عملیات</th>
                </tr>
              </thead>
            </DataTable>
          </div>
        </div>
      </div>

      {editUser && (
        <>
          <div className="modal-backdrop show users-modal-backdrop" onClick={closeModal} />
          <div className="modal show d-block users-modal access-levels-modal" tabIndex="-1">
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable modal-lg">
              <form
                ref={editFormRef}
                className="modal-content"
                onSubmit={handleSubmit}
                noValidate
              >
                <div className="modal-header">
                  <h5 className="modal-title">ویرایش دسترسی‌ها</h5>
                  <button
                    type="button"
                    className="btn-close"
                    aria-label="بستن"
                    onClick={closeModal}
                  />
                </div>
                <div className="modal-body">
                  {formError && <div className="alert alert-danger py-2">{formError}</div>}

                  <p className="text-muted small mb-3">
                    کاربر: <strong>{editUser.fullName}</strong> ({editUser.userName})
                    <br />
                    نقش: <strong>{editUser.roleName}</strong>
                  </p>

                  <div className="row g-3">
                    <div className="col-12">
                      <label className="form-label mb-1">دسترسی‌ها</label>
                      <PermissionTree
                        tree={permissionTree}
                        value={form.permissions}
                        hasFullAccess={form.hasFullAccess}
                        onChange={({ permissions, hasFullAccess }) =>
                          setForm((prev) => ({ ...prev, permissions, hasFullAccess }))
                        }
                      />
                    </div>
                  </div>
                </div>
                <div className="modal-footer">
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    onClick={closeModal}
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
    </div>
  )
}

export default AccessLevelsPage
