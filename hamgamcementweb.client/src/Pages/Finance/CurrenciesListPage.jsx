import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import Icon from '../../components/common/Icon'
import DataTable from '../../lib/dataTableSetup'
import { usePageCrud } from '../../permissions/usePageCrud'
import {
  createCurrency,
  createCurrenciesDataTableAjax,
  deleteCurrency,
  fetchBaseCurrency,
  setBaseCurrency,
  updateCurrency,
  updateExchangeRate,
} from '../../services/currenciesApi'

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
  name: '',
  symbol: '',
  currencyCode: '',
  description: '',
  decimalPlaces: 0,
  isBaseCurrency: false,
  isActive: true,
  baseUnitsPerUnit: '',
  changeReason: '',
}

function formatRate(value) {
  if (value == null || value === '') return '—'
  return Number(value).toLocaleString('fa-IR', { maximumFractionDigits: 8 })
}

function formatDate(value) {
  if (!value) return '—'
  return new Date(value).toLocaleString('fa-IR')
}

function CurrenciesListPage() {
  const tableRef = useRef(null)
  const { canCreate, canEdit, canDelete, can } = usePageCrud('/currencies/list')
  const [loadError, setLoadError] = useState('')
  const [baseCurrency, setBaseCurrency] = useState(null)
  const [showCreate, setShowCreate] = useState(false)
  const [editRow, setEditRow] = useState(null)
  const [deleteRow, setDeleteRow] = useState(null)
  const [rateRow, setRateRow] = useState(null)
  const [setBaseRow, setSetBaseRow] = useState(null)
  const [formError, setFormError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [createForm, setCreateForm] = useState(emptyForm)
  const [editForm, setEditForm] = useState(emptyForm)
  const [rateForm, setRateForm] = useState({
    baseUnitsPerUnit: '',
    changeReason: '',
  })

  const hasBaseCurrency = Boolean(baseCurrency)

  const loadBaseCurrency = useCallback(async () => {
    try {
      const data = await fetchBaseCurrency()
      setBaseCurrency(data)
    } catch {
      setBaseCurrency(null)
    }
  }, [])

  useEffect(() => {
    loadBaseCurrency()
  }, [loadBaseCurrency])

  const reloadTable = useCallback(() => {
    tableRef.current?.dt()?.ajax.reload(null, false)
  }, [])

  const openCreate = useCallback(() => {
    setFormError('')
    setShowCreate(true)
    setCreateForm({
      ...emptyForm,
      isBaseCurrency: !hasBaseCurrency,
    })
  }, [hasBaseCurrency])

  const openEdit = useCallback((row) => {
    setFormError('')
    setEditRow(row)
    setEditForm({
      name: row.name,
      symbol: row.symbol,
      currencyCode: row.currencyCode,
      description: row.description ?? '',
      decimalPlaces: row.decimalPlaces ?? 0,
      isBaseCurrency: row.isBaseCurrency,
      isActive: row.isActive,
      baseUnitsPerUnit: '',
      changeReason: '',
    })
  }, [])

  const openDelete = useCallback((row) => {
    setFormError('')
    setDeleteRow(row)
  }, [])

  const openRate = useCallback((row) => {
    setFormError('')
    setRateRow(row)
    setRateForm({
      baseUnitsPerUnit: row.currentRate ?? '',
      changeReason: '',
    })
  }, [])

  const openSetBase = useCallback((row) => {
    setFormError('')
    setSetBaseRow(row)
  }, [])

  const closeModals = () => {
    setShowCreate(false)
    setEditRow(null)
    setDeleteRow(null)
    setRateRow(null)
    setSetBaseRow(null)
    setFormError('')
    setSubmitting(false)
  }

  const handleCreateSubmit = async (event) => {
    event.preventDefault()
    setSubmitting(true)
    setFormError('')

    try {
      const payload = {
        name: createForm.name,
        symbol: createForm.symbol,
        currencyCode: createForm.currencyCode.toUpperCase(),
        description: createForm.description || null,
        decimalPlaces: Number(createForm.decimalPlaces),
        isBaseCurrency: createForm.isBaseCurrency,
        isActive: createForm.isActive,
      }

      if (!createForm.isBaseCurrency && createForm.baseUnitsPerUnit) {
        payload.baseUnitsPerUnit = Number(createForm.baseUnitsPerUnit)
        payload.changeReason = createForm.changeReason || null
      }

      await createCurrency(payload)
      closeModals()
      await loadBaseCurrency()
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
      await updateCurrency(editRow.currencyId, {
        name: editForm.name,
        symbol: editForm.symbol,
        currencyCode: editForm.currencyCode.toUpperCase(),
        description: editForm.description || null,
        decimalPlaces: Number(editForm.decimalPlaces),
        isBaseCurrency: editForm.isBaseCurrency,
        isActive: editForm.isActive,
      })
      closeModals()
      await loadBaseCurrency()
      reloadTable()
    } catch (error) {
      setFormError(error.message)
      setSubmitting(false)
    }
  }

  const handleRateSubmit = async (event) => {
    event.preventDefault()
    if (!rateRow) return

    setSubmitting(true)
    setFormError('')

    try {
      await updateExchangeRate(rateRow.currencyId, {
        baseUnitsPerUnit: Number(rateForm.baseUnitsPerUnit),
        changeReason: rateForm.changeReason || null,
      })
      closeModals()
      reloadTable()
    } catch (error) {
      setFormError(error.message)
      setSubmitting(false)
    }
  }

  const handleSetBaseConfirm = async () => {
    if (!setBaseRow) return

    setSubmitting(true)
    setFormError('')

    try {
      await setBaseCurrency(setBaseRow.currencyId)
      closeModals()
      await loadBaseCurrency()
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
      await deleteCurrency(deleteRow.currencyId)
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
      ajax: createCurrenciesDataTableAjax(setLoadError),
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
        bottomEnd: {
          paging: { firstLast: true, previousNext: true, numbers: 5 },
        },
      },
      columns: [
        { data: 'rowNumber', name: 'rowNumber' },
        { data: 'name', name: 'name' },
        { data: 'symbol', name: 'symbol' },
        { data: 'currencyCode', name: 'currencyCode' },
        { data: 'isBaseCurrency', name: 'isBaseCurrency' },
        { data: 'currentRate', name: 'currentRate' },
        { data: 'decimalPlaces', name: 'decimalPlaces' },
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
        {
          targets: 4,
          className: 'text-center',
          render: (data) =>
            data
              ? '<span class="badge badge-active">پایه</span>'
              : '<span class="text-muted">—</span>',
        },
        {
          targets: 5,
          className: 'text-center',
          render: (data, _type, row) =>
            row.isBaseCurrency ? '—' : formatRate(data),
        },
        { targets: 6, className: 'text-center' },
        { targets: 7, className: 'text-center' },
        {
          targets: 8,
          orderable: false,
          searchable: false,
          className: 'text-center all dt-actions-col',
          width: '140px',
        },
      ],
    }),
    [],
  )

  const actionSlots = useMemo(
    () => ({
      8: (_data, _type, row) => (
        <div className="dt-actions">
          {!row.isBaseCurrency && can('setRate') && (
            <button
              type="button"
              className="dt-action-btn"
              title="ثبت نرخ"
              onClick={() => openRate(row)}
            >
              <Icon name="exchange" />
            </button>
          )}
          {!row.isBaseCurrency && can('setBase') && (
            <button
              type="button"
              className="dt-action-btn"
              title="تعیین به‌عنوان ارز پایه"
              onClick={() => openSetBase(row)}
            >
              <Icon name="star" />
            </button>
          )}
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
          {!row.isBaseCurrency && canDelete && (
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
    [openEdit, openDelete, openRate, openSetBase, canEdit, canDelete, can],
  )

  const rateHint = baseCurrency
    ? `چند واحد ${baseCurrency.name} معادل ۱ واحد این ارز است`
    : 'ابتدا ارز پایه را تعریف کنید'

  return (
    <div className="users-page">
      <div className="content-card card border-0 h-100">
        <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between gap-3 flex-wrap">
          <div>
            <h2 className="card-title mb-1">لیست ارزها</h2>
            {baseCurrency ? (
              <p className="text-muted small mb-0">
                ارز پایه: <strong>{baseCurrency.name}</strong> ({baseCurrency.currencyCode})
              </p>
            ) : (
              <p className="text-muted small mb-0">
                هنوز ارز پایه تعریف نشده — اولین ارز را به‌عنوان پایه ثبت کنید.
              </p>
            )}
          </div>
          {canCreate && (
            <button
              type="button"
              className="btn btn-sm btn-accent btn-users-new d-inline-flex align-items-center gap-2"
              title="ارز جدید"
              onClick={openCreate}
            >
              <Icon name="plus" />
              <span>ارز جدید</span>
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
                  <th>نماد</th>
                  <th>کد</th>
                  <th>نوع</th>
                  <th>نرخ جاری</th>
                  <th>اعشار</th>
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
                  <h5 className="modal-title">ارز جدید</h5>
                  <button type="button" className="btn-close" aria-label="بستن" onClick={closeModals} />
                </div>
                <div className="modal-body">
                  {formError && <div className="alert alert-danger py-2">{formError}</div>}
                  <div className="row g-3 mb-3">
                    <div className="col-md-6">
                      <label className="form-label">نام</label>
                      <input
                        type="text"
                        className="form-control"
                        value={createForm.name}
                        onChange={(e) => setCreateForm((p) => ({ ...p, name: e.target.value }))}
                        required
                      />
                    </div>
                    <div className="col-md-6">
                      <label className="form-label">نماد</label>
                      <input
                        type="text"
                        className="form-control"
                        value={createForm.symbol}
                        onChange={(e) => setCreateForm((p) => ({ ...p, symbol: e.target.value }))}
                        required
                      />
                    </div>
                  </div>
                  <div className="row g-3 mb-3">
                    <div className="col-md-6">
                      <label className="form-label">کد (ISO)</label>
                      <input
                        type="text"
                        className="form-control text-uppercase"
                        maxLength={3}
                        value={createForm.currencyCode}
                        onChange={(e) => setCreateForm((p) => ({ ...p, currencyCode: e.target.value }))}
                        required
                      />
                    </div>
                    <div className="col-md-6">
                      <label className="form-label">تعداد اعشار</label>
                      <input
                        type="number"
                        min={0}
                        max={8}
                        className="form-control"
                        value={createForm.decimalPlaces}
                        onChange={(e) => setCreateForm((p) => ({ ...p, decimalPlaces: e.target.value }))}
                      />
                    </div>
                  </div>
                  <div className="mb-3">
                    <label className="form-label">توضیحات</label>
                    <input
                      type="text"
                      className="form-control"
                      value={createForm.description}
                      onChange={(e) => setCreateForm((p) => ({ ...p, description: e.target.value }))}
                    />
                  </div>
                  {!hasBaseCurrency && (
                    <div className="form-check form-switch mb-3">
                      <input
                        className="form-check-input"
                        type="checkbox"
                        id="create-currency-is-base"
                        checked={createForm.isBaseCurrency}
                        onChange={(e) =>
                          setCreateForm((p) => ({ ...p, isBaseCurrency: e.target.checked }))
                        }
                      />
                      <label className="form-check-label" htmlFor="create-currency-is-base">
                        ارز پایه سیستم
                      </label>
                    </div>
                  )}
                  {!createForm.isBaseCurrency && hasBaseCurrency && (
                    <>
                      <div className="mb-3">
                        <label className="form-label">نرخ اولیه ({rateHint})</label>
                        <input
                          type="number"
                          step="any"
                          min="0"
                          className="form-control"
                          value={createForm.baseUnitsPerUnit}
                          onChange={(e) =>
                            setCreateForm((p) => ({ ...p, baseUnitsPerUnit: e.target.value }))
                          }
                        />
                      </div>
                      <div className="mb-3">
                        <label className="form-label">دلیل تغییر</label>
                        <input
                          type="text"
                          className="form-control"
                          value={createForm.changeReason}
                          onChange={(e) =>
                            setCreateForm((p) => ({ ...p, changeReason: e.target.value }))
                          }
                        />
                      </div>
                    </>
                  )}
                  <div className="form-check form-switch">
                    <input
                      className="form-check-input"
                      type="checkbox"
                      id="create-currency-is-active"
                      checked={createForm.isActive}
                      onChange={(e) => setCreateForm((p) => ({ ...p, isActive: e.target.checked }))}
                    />
                    <label className="form-check-label" htmlFor="create-currency-is-active">
                      فعال
                    </label>
                  </div>
                </div>
                <div className="modal-footer">
                  <button type="button" className="btn btn-outline-secondary" onClick={closeModals}>
                    انصراف
                  </button>
                  <button type="submit" className="btn btn-accent" disabled={submitting}>
                    {submitting ? 'در حال ایجاد...' : 'ایجاد ارز'}
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
                  <h5 className="modal-title">ویرایش ارز</h5>
                  <button type="button" className="btn-close" aria-label="بستن" onClick={closeModals} />
                </div>
                <div className="modal-body">
                  {formError && <div className="alert alert-danger py-2">{formError}</div>}
                  <div className="row g-3 mb-3">
                    <div className="col-md-6">
                      <label className="form-label">نام</label>
                      <input
                        type="text"
                        className="form-control"
                        value={editForm.name}
                        onChange={(e) => setEditForm((p) => ({ ...p, name: e.target.value }))}
                        required
                      />
                    </div>
                    <div className="col-md-6">
                      <label className="form-label">نماد</label>
                      <input
                        type="text"
                        className="form-control"
                        value={editForm.symbol}
                        onChange={(e) => setEditForm((p) => ({ ...p, symbol: e.target.value }))}
                        required
                      />
                    </div>
                  </div>
                  <div className="row g-3 mb-3">
                    <div className="col-md-6">
                      <label className="form-label">کد (ISO)</label>
                      <input
                        type="text"
                        className="form-control text-uppercase"
                        maxLength={3}
                        value={editForm.currencyCode}
                        onChange={(e) => setEditForm((p) => ({ ...p, currencyCode: e.target.value }))}
                        required
                      />
                    </div>
                    <div className="col-md-6">
                      <label className="form-label">تعداد اعشار</label>
                      <input
                        type="number"
                        min={0}
                        max={8}
                        className="form-control"
                        value={editForm.decimalPlaces}
                        onChange={(e) => setEditForm((p) => ({ ...p, decimalPlaces: e.target.value }))}
                      />
                    </div>
                  </div>
                  <div className="mb-3">
                    <label className="form-label">توضیحات</label>
                    <input
                      type="text"
                      className="form-control"
                      value={editForm.description}
                      onChange={(e) => setEditForm((p) => ({ ...p, description: e.target.value }))}
                    />
                  </div>
                  {editForm.isBaseCurrency && (
                    <p className="text-muted small">این ارز، ارز پایه سیستم است.</p>
                  )}
                  <div className="form-check form-switch">
                    <input
                      className="form-check-input"
                      type="checkbox"
                      id="edit-currency-is-active"
                      checked={editForm.isActive}
                      onChange={(e) => setEditForm((p) => ({ ...p, isActive: e.target.checked }))}
                    />
                    <label className="form-check-label" htmlFor="edit-currency-is-active">
                      فعال
                    </label>
                  </div>
                </div>
                <div className="modal-footer">
                  <button type="button" className="btn btn-outline-secondary" onClick={closeModals}>
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

      {rateRow && (
        <>
          <div className="modal-backdrop show users-modal-backdrop" onClick={closeModals} />
          <div className="modal show d-block users-modal" tabIndex="-1" role="dialog">
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable">
              <form className="modal-content" onSubmit={handleRateSubmit}>
                <div className="modal-header">
                  <h5 className="modal-title">ثبت نرخ — {rateRow.name}</h5>
                  <button type="button" className="btn-close" aria-label="بستن" onClick={closeModals} />
                </div>
                <div className="modal-body">
                  {formError && <div className="alert alert-danger py-2">{formError}</div>}
                  <p className="text-muted small">{rateHint}</p>
                  {rateRow.currentRate != null && (
                    <p className="small mb-3">
                      نرخ فعلی: <strong>{formatRate(rateRow.currentRate)}</strong>
                      {rateRow.rateEffectiveFrom && (
                        <span className="text-muted"> — از {formatDate(rateRow.rateEffectiveFrom)}</span>
                      )}
                    </p>
                  )}
                  <div className="mb-3">
                    <label className="form-label">نرخ جدید</label>
                    <input
                      type="number"
                      step="any"
                      min="0"
                      className="form-control"
                      value={rateForm.baseUnitsPerUnit}
                      onChange={(e) =>
                        setRateForm((p) => ({ ...p, baseUnitsPerUnit: e.target.value }))
                      }
                      required
                    />
                  </div>
                  <div className="mb-3">
                    <label className="form-label">دلیل تغییر</label>
                    <input
                      type="text"
                      className="form-control"
                      value={rateForm.changeReason}
                      onChange={(e) =>
                        setRateForm((p) => ({ ...p, changeReason: e.target.value }))
                      }
                    />
                  </div>
                </div>
                <div className="modal-footer">
                  <button type="button" className="btn btn-outline-secondary" onClick={closeModals}>
                    انصراف
                  </button>
                  <button type="submit" className="btn btn-accent" disabled={submitting}>
                    {submitting ? 'در حال ثبت...' : 'ثبت نرخ'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </>
      )}

      {setBaseRow && (
        <>
          <div className="modal-backdrop show users-modal-backdrop" onClick={closeModals} />
          <div className="modal show d-block users-modal" tabIndex="-1" role="dialog">
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable">
              <div className="modal-content">
                <div className="modal-header">
                  <h5 className="modal-title">تعیین ارز پایه</h5>
                  <button type="button" className="btn-close" aria-label="بستن" onClick={closeModals} />
                </div>
                <div className="modal-body">
                  {formError && <div className="alert alert-danger py-2">{formError}</div>}
                  <p className="mb-0">
                    آیا <strong>{setBaseRow.name}</strong> به‌عنوان ارز پایه سیستم تنظیم شود؟
                    {baseCurrency && (
                      <>
                        {' '}
                        ارز پایه فعلی ({baseCurrency.name}) از این نقش خارج می‌شود.
                      </>
                    )}
                  </p>
                </div>
                <div className="modal-footer">
                  <button type="button" className="btn btn-outline-secondary" onClick={closeModals}>
                    انصراف
                  </button>
                  <button
                    type="button"
                    className="btn btn-accent"
                    onClick={handleSetBaseConfirm}
                    disabled={submitting}
                  >
                    {submitting ? 'در حال ذخیره...' : 'تأیید'}
                  </button>
                </div>
              </div>
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
                  <h5 className="modal-title">حذف ارز</h5>
                  <button type="button" className="btn-close" aria-label="بستن" onClick={closeModals} />
                </div>
                <div className="modal-body">
                  {formError && <div className="alert alert-danger py-2">{formError}</div>}
                  <p className="mb-0">
                    آیا از حذف ارز <strong>{deleteRow.name}</strong> اطمینان دارید؟
                    فقط ارزهای بدون نرخ تبدیل قابل حذف هستند.
                  </p>
                </div>
                <div className="modal-footer">
                  <button type="button" className="btn btn-outline-secondary" onClick={closeModals}>
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

export default CurrenciesListPage
