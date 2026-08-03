import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import AmountField from '../../components/common/AmountField'
import Icon from '../../components/common/Icon'
import {
  useModalKeyboardShortcuts,
  usePageCreateShortcut,
  useModalAutoFocus,
} from '../../hooks/useModalKeyboardShortcuts'
import { showAppToast } from '../../lib/appToast'
import DataTable from '../../lib/dataTableSetup'
import { createServerSideTableOptions } from '../../lib/dataTableOptions'
import { makeAmountCurrencyRender, makeSignedBalanceRender } from '../../lib/currencyFormat'
import { persianValidity, validateFormPersian } from '../../lib/persianFormValidity'
import { usePageCrud } from '../../permissions/usePageCrud'
import { fetchBaseCurrency } from '../../services/currenciesApi'
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

function accountStatusBadge(code, label) {
  const cls =
    code === 'debtor'
      ? 'badge-debtor'
      : code === 'creditor'
        ? 'badge-creditor'
        : 'badge-settled'
  return `<span class="badge ${cls}">${label}</span>`
}

function SupplierFormFields({ form, setForm, idPrefix, baseCurrencySymbol }) {
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
      <div className="col-md-8">
        <label className="form-label mb-1">نام</label>
        <input
          type="text"
          className="form-control"
          value={form.name}
          required
          {...persianValidity('لطفاً نام تأمین‌کننده را وارد کنید.')}
          onChange={(e) => {
            e.target.setCustomValidity('')
            setForm((prev) => ({ ...prev, name: e.target.value }))
          }}
        />
      </div>
      <div className="col-md-6">
        <label className="form-label mb-1">تلفن</label>
        <input
          type="text"
          dir="ltr"
          className="form-control"
          value={form.phoneNumber}
          required
          {...persianValidity('لطفاً تلفن تأمین‌کننده را وارد کنید.')}
          onChange={(e) => {
            e.target.setCustomValidity('')
            setForm((prev) => ({ ...prev, phoneNumber: e.target.value }))
          }}
        />
      </div>
      <div className="col-md-6">
        <label className="form-label mb-1">نوع</label>
        <select
          className="form-select"
          value={form.supplierType}
          onChange={(e) => setForm((prev) => ({ ...prev, supplierType: e.target.value }))}
        >
          {PERSON_TYPE_OPTIONS.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
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
        <label className="form-label mb-1">شهر</label>
        <input
          type="text"
          className="form-control"
          value={form.city}
          onChange={(e) => setForm((prev) => ({ ...prev, city: e.target.value }))}
        />
      </div>
      <div className="col-md-6">
        <label className="form-label mb-1">کشور</label>
        <input
          type="text"
          className="form-control"
          value={form.country}
          onChange={(e) => setForm((prev) => ({ ...prev, country: e.target.value }))}
        />
      </div>
      <div className="col-md-6">
        <label className="form-label mb-1">موجودی اولیه</label>
        <AmountField
          symbol={baseCurrencySymbol}
          step={100}
          value={form.initialBalance}
          onChange={(value) => setForm((prev) => ({ ...prev, initialBalance: value }))}
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

function SuppliersPage() {
  const navigate = useNavigate()
  const tableRef = useRef(null)
  const createFormRef = useRef(null)
  const editFormRef = useRef(null)
  const { canCreate, canEdit, canDelete, canView } = usePageCrud('/people/suppliers')
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
  const [baseCurrencySymbol, setBaseCurrencySymbol] = useState('')
  const currencySymbolRef = useRef('')

  useEffect(() => {
    let cancelled = false
    fetchBaseCurrency()
      .then((currency) => {
        if (!cancelled) {
          setBaseCurrencySymbol(currency?.symbol ?? '')
        }
      })
      .catch(() => {
        if (!cancelled) setBaseCurrencySymbol('')
      })
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    currencySymbolRef.current = baseCurrencySymbol
    const dt = tableRef.current?.dt()
    if (dt && baseCurrencySymbol) {
      dt.rows().invalidate('data').draw(false)
    }
  }, [baseCurrencySymbol])

  const amountCurrencyRender = useMemo(
    () => makeAmountCurrencyRender(() => currencySymbolRef.current),
    [],
  )

  const signedBalanceRender = useMemo(
    () => makeSignedBalanceRender(() => currencySymbolRef.current),
    [],
  )

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

  const openView = useCallback(
    (row) => {
      navigate(`/people/suppliers/${row.supplierId}`)
    },
    [navigate],
  )

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

  const handleTableLoaded = useCallback((json) => {
    if (json.currencySymbol) {
      currencySymbolRef.current = json.currencySymbol
      setBaseCurrencySymbol(json.currencySymbol)
    }
  }, [])

  const tableOptions = useMemo(
    () =>
      createServerSideTableOptions({
        ajax: createSuppliersDataTableAjax(setLoadError, handleTableLoaded),
        order: [[1, 'asc']],
        rowCallback: (row, data) => {
          if (data.isDeleted) {
            row.classList.add('dt-row-deleted')
          } else {
            row.classList.remove('dt-row-deleted')
          }
        },
        columns: [
          { data: 'rowNumber', name: 'rowNumber' },
          {
            data: 'name',
            name: 'name',
            render: (data, type, row) =>
              row.isDeleted
                ? `<span class="dt-cell-deleted">${data ?? ''}</span>`
                : (data ?? ''),
          },
          {
            data: 'accountCode',
            name: 'accountCode',
            render: (data) => data || '—',
          },
          { data: 'phoneNumber', name: 'phoneNumber' },
          { data: 'initialBalance', name: 'initialBalance', render: amountCurrencyRender },
          { data: 'totalPurchase', name: 'totalPurchase', render: amountCurrencyRender },
          { data: 'totalPayment', name: 'totalPayment', render: amountCurrencyRender },
          { data: 'balance', name: 'balance', render: signedBalanceRender },
          {
            data: 'accountStatus',
            name: 'accountStatus',
            render: (_data, _type, row) =>
              accountStatusBadge(row.accountStatusCode, row.accountStatus),
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
          { targets: [2, 4, 5, 6, 7, 8], className: 'text-center' },
          {
            targets: 9,
            orderable: false,
            searchable: false,
            className: 'text-center all dt-actions-col',
            width: '130px',
          },
        ],
      }),
    [amountCurrencyRender, signedBalanceRender, handleTableLoaded],
  )

  const actionSlots = useMemo(
    () => ({
      9: (_data, _type, row) => {
        const canDeleteRow = row.accountStatusCode === 'settled'
        return (
        <div className="dt-actions">
          {canView && (
            <button
              type="button"
              className="dt-action-btn"
              title="مشاهده"
              onClick={() => openView(row)}
            >
              <Icon name="eye" />
            </button>
          )}
          {canEdit && !row.isDeleted && (
            <button
              type="button"
              className="dt-action-btn"
              title="ویرایش"
              onClick={() => openEdit(row)}
            >
              <Icon name="edit" />
            </button>
          )}
          {canDelete && !row.isDeleted && (
            <button
              type="button"
              className="dt-action-btn btn-delete"
              title={canDeleteRow ? 'حذف' : 'فقط تأمین‌کنندگان تسویه‌شده قابل حذف هستند'}
              disabled={!canDeleteRow}
              onClick={() => canDeleteRow && openDelete(row)}
            >
              <Icon name="trash" />
            </button>
          )}
        </div>
        )
      },
    }),
    [openEdit, openDelete, openView, canEdit, canDelete, canView],
  )

  return (
    <div className="users-page">
      <div className="content-card card border-0 h-100">
        <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between gap-3 flex-wrap">
          <h2 className="card-title mb-0">تأمین‌کنندگان</h2>
          {canCreate && (
            <button
              type="button"
              className="btn btn-sm btn-accent btn-users-new d-inline-flex align-items-center gap-2"
              title="تأمین‌کننده جدید (Ctrl+N)"
              onClick={openCreate}
            >
              <Icon name="plus" />
              <span>تأمین‌کننده جدید</span>
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
                  <th>کد حساب</th>
                  <th>تلفن</th>
                  <th>موجودی اولیه</th>
                  <th>کل خرید</th>
                  <th>کل پرداخت</th>
                  <th>بلانس</th>
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
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable modal-lg">
              <form ref={createFormRef} className="modal-content" onSubmit={handleCreateSubmit} noValidate>
                <div className="modal-header">
                  <h5 className="modal-title">تأمین‌کننده جدید</h5>
                  <button type="button" className="btn-close" aria-label="بستن" onClick={closeModals} />
                </div>
                <div className="modal-body">
                  {formError && <div className="alert alert-danger py-2">{formError}</div>}
                  <SupplierFormFields
                    form={createForm}
                    setForm={setCreateForm}
                    idPrefix="create-supplier"
                    baseCurrencySymbol={baseCurrencySymbol}
                  />
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
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable modal-lg">
              <form ref={editFormRef} className="modal-content" onSubmit={handleEditSubmit} noValidate>
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
                  <SupplierFormFields
                    form={editForm}
                    setForm={setEditForm}
                    idPrefix="edit-supplier"
                    baseCurrencySymbol={baseCurrencySymbol}
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
