import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import Icon from '../../components/common/Icon'
import JalaliDateField from '../../components/common/JalaliDateField'
import DataTable from '../../lib/dataTableSetup'
import { usePageCrud } from '../../permissions/usePageCrud'
import { dataTableLanguage, formatAmount, formatJalaliDate } from './CrudTablePage'
import {
  fetchCurrencyOptions,
  fetchExpenseCategoryOptions,
  fetchTripOptions,
  fetchVehicleOptions,
  getInvoice,
  invoicesApi,
} from '../../services/transportApi'

const emptyHeader = {
  invoiceNumber: '',
  vehicleId: '',
  transportTripId: '',
  invoiceDate: '',
  description: '',
}

const emptyLine = {
  transportExpenseId: null,
  expensesCategoryId: '',
  title: '',
  amount: '',
  currencyId: '',
  expenseDate: '',
  description: '',
}

function TransportInvoicesPage() {
  const tableRef = useRef(null)
  const { canCreate, canEdit, canDelete } = usePageCrud('/transport/invoices')
  const [loadError, setLoadError] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editId, setEditId] = useState(null)
  const [deleteRow, setDeleteRow] = useState(null)
  const [formError, setFormError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [header, setHeader] = useState(emptyHeader)
  const [lines, setLines] = useState([{ ...emptyLine }])
  const [vehicles, setVehicles] = useState([])
  const [trips, setTrips] = useState([])
  const [categories, setCategories] = useState([])
  const [currencies, setCurrencies] = useState([])

  useEffect(() => {
    fetchVehicleOptions().then(setVehicles).catch(() => setVehicles([]))
    fetchTripOptions().then(setTrips).catch(() => setTrips([]))
    fetchExpenseCategoryOptions().then(setCategories).catch(() => setCategories([]))
    fetchCurrencyOptions().then(setCurrencies).catch(() => setCurrencies([]))
  }, [])

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
    setFormError('')
    setHeader(emptyHeader)
    setLines([{ ...emptyLine }])
    setEditId(null)
    setShowForm(true)
  }, [])

  const openEdit = useCallback(async (row) => {
    setFormError('')
    try {
      const invoice = await getInvoice(row.transportInvoiceId)
      setHeader({
        invoiceNumber: invoice.invoiceNumber,
        vehicleId: invoice.vehicleId,
        transportTripId: invoice.transportTripId ?? '',
        invoiceDate: String(invoice.invoiceDate).slice(0, 10),
        description: invoice.description ?? '',
      })
      setLines(
        (invoice.expenses ?? []).map((e) => ({
          transportExpenseId: e.transportExpenseId,
          expensesCategoryId: e.expensesCategoryId,
          title: e.title,
          amount: e.amount,
          currencyId: e.currencyId ?? '',
          expenseDate: e.expenseDate ? String(e.expenseDate).slice(0, 10) : '',
          description: e.description ?? '',
        })),
      )
      setEditId(invoice.transportInvoiceId)
      setShowForm(true)
    } catch (error) {
      setLoadError(error.message)
    }
  }, [])

  const handleHeaderChange = (name, value) => {
    setHeader((prev) => ({ ...prev, [name]: value }))
  }

  const handleLineChange = (index, name, value) => {
    setLines((prev) =>
      prev.map((line, i) => (i === index ? { ...line, [name]: value } : line)),
    )
  }

  const addLine = () => setLines((prev) => [...prev, { ...emptyLine }])

  const removeLine = (index) =>
    setLines((prev) => (prev.length > 1 ? prev.filter((_, i) => i !== index) : prev))

  const totalAmount = useMemo(
    () => lines.reduce((sum, line) => sum + (Number(line.amount) || 0), 0),
    [lines],
  )

  const handleSubmit = async (event) => {
    event.preventDefault()
    setSubmitting(true)
    setFormError('')

    try {
      const payload = {
        vehicleId: Number(header.vehicleId),
        transportTripId: header.transportTripId ? Number(header.transportTripId) : null,
        invoiceDate: header.invoiceDate,
        description: header.description || null,
        expenses: lines.map((line) => ({
          transportExpenseId: line.transportExpenseId,
          expensesCategoryId: Number(line.expensesCategoryId),
          title: line.title,
          amount: Number(line.amount),
          currencyId: line.currencyId ? Number(line.currencyId) : null,
          expenseDate: line.expenseDate || null,
          description: line.description || null,
        })),
      }

      if (editId) {
        await invoicesApi.update(editId, payload)
      } else {
        await invoicesApi.create(payload)
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
      await invoicesApi.remove(deleteRow.transportInvoiceId)
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
      ajax: invoicesApi.createDataTableAjax(setLoadError),
      paging: true,
      searching: true,
      ordering: true,
      info: true,
      scrollX: true,
      autoWidth: false,
      responsive: true,
      stripeClasses: ['odd', 'even'],
      order: [[4, 'desc']],
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
        { data: 'invoiceNumber', name: 'invoiceNumber' },
        { data: 'vehicleLabel', name: 'vehicleLabel' },
        {
          data: 'tripNumber',
          name: 'tripNumber',
          render: (data) => data ?? '—',
        },
        {
          data: 'invoiceDate',
          name: 'invoiceDate',
          render: (data) => formatJalaliDate(data),
        },
        {
          data: 'totalAmount',
          name: 'totalAmount',
          render: (data) => formatAmount(data),
        },
        { data: 'itemsCount', name: 'itemsCount' },
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
        { targets: [2, 3, 6], orderable: false },
        { targets: [4, 5, 6], className: 'text-center' },
        {
          targets: 7,
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
              onClick={() => setDeleteRow(row)}
            >
              <Icon name="trash" />
            </button>
          )}
        </div>
      ),
    }),
    [openEdit, canEdit, canDelete],
  )

  return (
    <div className="users-page">
      <div className="content-card card border-0 h-100">
        <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between gap-3 flex-wrap">
          <h2 className="card-title mb-0">فاکتورهای مصارف حمل و نقل</h2>
          {canCreate && (
            <button
              type="button"
              className="btn btn-sm btn-accent btn-users-new d-inline-flex align-items-center gap-2"
              title="فاکتور جدید"
              onClick={openCreate}
            >
              <Icon name="plus" />
              <span>فاکتور جدید</span>
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
                  <th>شماره فاکتور</th>
                  <th>وسیله نقلیه</th>
                  <th>سفر</th>
                  <th>تاریخ</th>
                  <th>جمع کل</th>
                  <th>تعداد ردیف</th>
                  <th>عملیات</th>
                </tr>
              </thead>
            </DataTable>
          </div>
        </div>
      </div>

      {showForm && (
        <>
          <div className="modal-backdrop show users-modal-backdrop" onClick={closeModals} />
          <div
            className="modal show d-block users-modal"
            tabIndex="-1"
            role="dialog"
            data-bs-focus="false"
          >
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable modal-xl">
              <form className="modal-content" onSubmit={handleSubmit}>
                <div className="modal-header">
                  <h5 className="modal-title">
                    {editId ? 'ویرایش فاکتور مصارف' : 'فاکتور مصارف جدید'}
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

                  <div className="row g-3 mb-4">
                    {editId && (
                      <div className="col-md-3">
                        <label className="form-label">شماره فاکتور</label>
                        <input
                          type="text"
                          className="form-control"
                          value={header.invoiceNumber}
                          readOnly
                          disabled
                        />
                      </div>
                    )}
                    <div className={editId ? 'col-md-3' : 'col-md-4'}>
                      <label className="form-label">وسیله نقلیه</label>
                      <select
                        className="form-select"
                        value={header.vehicleId}
                        required
                        onChange={(e) => handleHeaderChange('vehicleId', e.target.value)}
                      >
                        <option value="">انتخاب کنید...</option>
                        {vehicles.map((option) => (
                          <option key={option.value} value={option.value}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div className={editId ? 'col-md-3' : 'col-md-4'}>
                      <label className="form-label">سفر (اختیاری)</label>
                      <select
                        className="form-select"
                        value={header.transportTripId}
                        onChange={(e) => handleHeaderChange('transportTripId', e.target.value)}
                      >
                        <option value="">بدون سفر</option>
                        {trips.map((option) => (
                          <option key={option.value} value={option.value}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div className={editId ? 'col-md-3' : 'col-md-4'}>
                      <label className="form-label">تاریخ فاکتور (شمسی)</label>
                      <JalaliDateField
                        value={header.invoiceDate}
                        onChange={(next) => handleHeaderChange('invoiceDate', next)}
                        required
                      />
                    </div>
                    <div className="col-12">
                      <label className="form-label">توضیحات</label>
                      <input
                        type="text"
                        className="form-control"
                        value={header.description}
                        onChange={(e) => handleHeaderChange('description', e.target.value)}
                      />
                    </div>
                  </div>

                  <div className="d-flex align-items-center justify-content-between mb-2">
                    <h6 className="mb-0">ردیف‌های مصرف</h6>
                    <button
                      type="button"
                      className="btn btn-sm btn-outline-secondary d-inline-flex align-items-center gap-1"
                      onClick={addLine}
                    >
                      <Icon name="plus" />
                      <span>ردیف جدید</span>
                    </button>
                  </div>

                  <div className="table-responsive">
                    <table className="table align-middle">
                      <thead>
                        <tr>
                          <th style={{ minWidth: 160 }}>دسته‌بندی</th>
                          <th style={{ minWidth: 180 }}>عنوان</th>
                          <th style={{ minWidth: 120 }}>مبلغ</th>
                          <th style={{ minWidth: 140 }}>ارز</th>
                          <th style={{ minWidth: 140 }}>تاریخ</th>
                          <th style={{ minWidth: 160 }}>توضیحات</th>
                          <th />
                        </tr>
                      </thead>
                      <tbody>
                        {lines.map((line, index) => (
                          <tr key={index}>
                            <td>
                              <select
                                className="form-select form-select-sm"
                                value={line.expensesCategoryId}
                                required
                                onChange={(e) =>
                                  handleLineChange(index, 'expensesCategoryId', e.target.value)
                                }
                              >
                                <option value="">انتخاب کنید...</option>
                                {categories.map((option) => (
                                  <option key={option.value} value={option.value}>
                                    {option.label}
                                  </option>
                                ))}
                              </select>
                            </td>
                            <td>
                              <input
                                type="text"
                                className="form-control form-control-sm"
                                value={line.title}
                                required
                                onChange={(e) =>
                                  handleLineChange(index, 'title', e.target.value)
                                }
                              />
                            </td>
                            <td>
                              <input
                                type="number"
                                step="any"
                                min="0"
                                className="form-control form-control-sm"
                                value={line.amount}
                                required
                                onChange={(e) =>
                                  handleLineChange(index, 'amount', e.target.value)
                                }
                              />
                            </td>
                            <td>
                              <select
                                className="form-select form-select-sm"
                                value={line.currencyId}
                                onChange={(e) =>
                                  handleLineChange(index, 'currencyId', e.target.value)
                                }
                              >
                                <option value="">ارز پایه</option>
                                {currencies.map((option) => (
                                  <option key={option.value} value={option.value}>
                                    {option.label}
                                  </option>
                                ))}
                              </select>
                            </td>
                            <td>
                              <JalaliDateField
                                value={line.expenseDate}
                                onChange={(next) =>
                                  handleLineChange(index, 'expenseDate', next)
                                }
                                small
                                inputClass="form-control form-control-sm hc-jalali-input"
                              />
                            </td>
                            <td>
                              <input
                                type="text"
                                className="form-control form-control-sm"
                                value={line.description}
                                onChange={(e) =>
                                  handleLineChange(index, 'description', e.target.value)
                                }
                              />
                            </td>
                            <td className="text-center">
                              <button
                                type="button"
                                className="dt-action-btn btn-delete"
                                title="حذف ردیف"
                                onClick={() => removeLine(index)}
                                disabled={lines.length === 1}
                              >
                                <Icon name="trash" />
                              </button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                      <tfoot>
                        <tr>
                          <th colSpan={2} className="text-start">
                            جمع کل
                          </th>
                          <th>{formatAmount(totalAmount)}</th>
                          <th colSpan={4} />
                        </tr>
                      </tfoot>
                    </table>
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
                    {submitting ? 'در حال ذخیره...' : 'ذخیره فاکتور'}
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
          <div
            className="modal show d-block users-modal"
            tabIndex="-1"
            role="dialog"
            data-bs-focus="false"
          >
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable">
              <div className="modal-content">
                <div className="modal-header">
                  <h5 className="modal-title">حذف فاکتور</h5>
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
                    آیا از حذف فاکتور <strong>{deleteRow.invoiceNumber}</strong> به همراه تمام
                    ردیف‌های مصرف آن اطمینان دارید؟
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

export default TransportInvoicesPage
