import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import Icon from '../../components/common/Icon'
import JalaliDateField from '../../components/common/JalaliDateField'
import { useModalKeyboardShortcuts } from '../../hooks/useModalKeyboardShortcuts'
import { formatJalaliDate } from '../../lib/afghanSolarCalendar'
import DataTable from '../../lib/dataTableSetup'
import {
  amountRender,
  createServerSideTableOptions,
  dataTableLanguage,
  formatAmount,
} from '../../lib/dataTableOptions'
import { usePageCrud } from '../../permissions/usePageCrud'

export { dataTableLanguage, formatAmount }

export function formatDate(value) {
  if (!value) return '—'
  return String(value).slice(0, 10)
}

export { formatJalaliDate }

function buildInitialForm(fields) {
  const form = {}
  for (const field of fields) {
    if (field.default !== undefined) {
      form[field.name] = field.default
    } else if (field.type === 'switch') {
      form[field.name] = true
    } else if (field.type === 'multiselect') {
      form[field.name] = []
    } else {
      form[field.name] = ''
    }
  }
  return form
}

function rowToForm(fields, row) {
  const form = {}
  for (const field of fields) {
    let value = field.fromRow ? field.fromRow(row) : row[field.name]
    if (value === null || value === undefined) {
      value =
        field.type === 'switch' ? false : field.type === 'multiselect' ? [] : ''
    } else if (field.type === 'date' || field.type === 'jalali-date') {
      value = String(value).slice(0, 10)
    } else if (field.type === 'multiselect' && !Array.isArray(value)) {
      value = String(value)
        .split(/[,\s]+/)
        .map((x) => x.trim())
        .filter(Boolean)
    } else if (field.type === 'multiselect') {
      value = value.map(String)
    }
    form[field.name] = value
  }
  return form
}

function formToPayload(fields, form) {
  const payload = {}
  for (const field of fields) {
    if (field.skipOnSubmit || field.autoCode) {
      continue
    }
    const raw = form[field.name]
    switch (field.type) {
      case 'number':
        payload[field.name] = raw === '' || raw === null ? null : Number(raw)
        break
      case 'select':
        payload[field.name] =
          raw === '' || raw === null
            ? null
            : field.stringValue
              ? raw
              : Number(raw)
        break
      case 'multiselect':
        payload[field.name] = (Array.isArray(raw) ? raw : [])
          .map((v) => (field.stringValue ? String(v) : Number(v)))
          .filter((v) =>
            field.stringValue
              ? String(v).length > 0
              : Number.isFinite(v) && v > 0,
          )
        break
      case 'date':
      case 'jalali-date':
        payload[field.name] = raw === '' ? null : raw
        break
      case 'switch':
        payload[field.name] = Boolean(raw)
        break
      default:
        payload[field.name] = raw
    }
  }
  return payload
}

function FieldInput({ field, value, onChange, optionsMap, readOnly }) {
  if (field.type === 'readonly' || readOnly) {
    return (
      <input
        type="text"
        className="form-control"
        value={value ?? ''}
        readOnly
        disabled
      />
    )
  }

  const common = {
    className: field.type === 'select' ? 'form-select' : 'form-control',
    value: value ?? '',
    required: field.required,
    onChange: (e) => onChange(field.name, e.target.value),
  }

  switch (field.type) {
    case 'select': {
      const options = field.options ?? optionsMap[field.name] ?? []
      return (
        <select {...common}>
          <option value="">{field.placeholder ?? 'انتخاب کنید...'}</option>
          {options.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      )
    }
    case 'multiselect': {
      const options = field.options ?? optionsMap[field.name] ?? []
      const selected = new Set(
        (Array.isArray(value) ? value : []).map(String),
      )
      const toggle = (optionValue) => {
        const next = new Set(selected)
        const key = String(optionValue)
        if (next.has(key)) next.delete(key)
        else next.add(key)
        onChange(field.name, Array.from(next))
      }
      return (
        <div className="crud-multiselect border rounded px-2 py-2">
          {options.length === 0 ? (
            <div className="text-muted small py-1">موردی برای انتخاب نیست.</div>
          ) : (
            options.map((option) => {
              const key = String(option.value)
              const id = `crud-ms-${field.name}-${key}`
              return (
                <div className="form-check" key={key}>
                  <input
                    className="form-check-input"
                    type="checkbox"
                    id={id}
                    checked={selected.has(key)}
                    onChange={() => toggle(option.value)}
                  />
                  <label className="form-check-label" htmlFor={id}>
                    {option.label}
                  </label>
                </div>
              )
            })
          )}
        </div>
      )
    }
    case 'textarea':
      return <textarea {...common} rows={field.rows ?? 2} />
    case 'number':
      return <input type="number" step={field.step ?? 'any'} {...common} />
    case 'date':
      return <input type="date" {...common} />
    case 'jalali-date':
      return (
        <JalaliDateField
          value={value}
          onChange={(next) => onChange(field.name, next)}
          required={field.required}
          placeholder={field.placeholder}
        />
      )
    default:
      return <input type="text" {...common} />
  }
}

/**
 * صفحه CRUD مشترک بخش حمل و نقل — جدول سرور-ساید + مودال‌های ایجاد/ویرایش/حذف
 */
function CrudTablePage({
  title,
  createLabel,
  deleteConfirmText,
  api,
  idField,
  nameField,
  columns,
  fields,
  defaultOrder = [[1, 'asc']],
  searching = true,
  extraRowActions,
  headerExtra,
  embedded = false,
  permissionPath,
  onFormChange,
  canEditRow = () => true,
  canDeleteRow = () => true,
}) {
  const { canCreate, canEdit, canDelete } = usePageCrud(permissionPath ?? '')
  const permissionsEnabled = Boolean(permissionPath)
  const tableRef = useRef(null)
  const formRef = useRef(null)
  const [loadError, setLoadError] = useState('')
  const [showCreate, setShowCreate] = useState(false)
  const [editRow, setEditRow] = useState(null)
  const [deleteRow, setDeleteRow] = useState(null)
  const [formError, setFormError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [form, setForm] = useState(() => buildInitialForm(fields))
  const [optionsMap, setOptionsMap] = useState({})

  useEffect(() => {
    let cancelled = false

    async function loadAll() {
      const loaders = fields.filter(
        (f) =>
          (f.type === 'select' || f.type === 'multiselect') && f.loadOptions,
      )
      for (const field of loaders) {
        try {
          const options = await field.loadOptions()
          if (!cancelled) {
            setOptionsMap((prev) => ({ ...prev, [field.name]: options }))
          }
        } catch {
          // خطای بارگذاری گزینه‌ها مانع نمایش صفحه نمی‌شود
        }
      }
    }

    loadAll()
    return () => {
      cancelled = true
    }
  }, [fields])

  const reloadTable = useCallback(() => {
    tableRef.current?.dt()?.ajax.reload(null, false)
  }, [])

  const openCreate = useCallback(() => {
    setFormError('')
    setForm(buildInitialForm(fields))
    setShowCreate(true)
  }, [fields])

  const openEdit = useCallback(
    (row) => {
      setFormError('')
      setForm(rowToForm(fields, row))
      setEditRow(row)
    },
    [fields],
  )

  const openDelete = useCallback((row) => {
    setFormError('')
    setDeleteRow(row)
  }, [])

  const closeModals = useCallback(() => {
    setShowCreate(false)
    setEditRow(null)
    setDeleteRow(null)
    setFormError('')
    setSubmitting(false)
  }, [])

  const modalOpen = showCreate || editRow || deleteRow
  const canSaveForm = showCreate || editRow

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

  const handleFieldChange = useCallback(
    (name, value) => {
      setForm((prev) => {
        let next = { ...prev, [name]: value }
        const patch = onFormChange?.(name, value, prev)
        if (patch) {
          next = { ...next, ...patch }
        }
        return next
      })
    },
    [onFormChange],
  )

  const visibleFields = useMemo(
    () =>
      fields.filter((field) => {
        if (field.showOnlyOnEdit && !editRow) return false
        if (field.hideOnCreate && showCreate && !editRow) return false
        if (field.showWhen && !field.showWhen(form)) return false
        return true
      }),
    [fields, editRow, showCreate, form],
  )

  const handleSubmit = async (event) => {
    event.preventDefault()
    setSubmitting(true)
    setFormError('')

    try {
      const payload = formToPayload(fields, form)
      if (editRow) {
        await api.update(editRow[idField], payload)
      } else {
        await api.create(payload)
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
      await api.remove(deleteRow[idField])
      closeModals()
      reloadTable()
    } catch (error) {
      setFormError(error.message)
      setSubmitting(false)
    }
  }

  const actionsIndex = columns.length + 1

  const tableOptions = useMemo(
    () =>
      createServerSideTableOptions({
        ajax: api.createDataTableAjax(setLoadError),
        searching,
        order: defaultOrder,
        columns: [
          { data: 'rowNumber', name: 'rowNumber' },
          ...columns.map((col) => ({
            data: col.data,
            name: col.data,
            render:
              col.render ??
              (col.type === 'number' || col.format === 'amount'
                ? amountRender
                : undefined),
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
          ...columns
            .map((col, index) => ({
              targets: index + 1,
              orderable: col.orderable !== false,
              className: col.className,
            }))
            .filter((def) => def.orderable === false || def.className),
          {
            targets: actionsIndex,
            orderable: false,
            searchable: false,
            className: 'text-center all dt-actions-col',
            width: extraRowActions ? '130px' : '100px',
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
          {extraRowActions?.(row, reloadTable)}
          {(!permissionsEnabled || canEdit) && canEditRow(row) && (
            <button
              type="button"
              className="dt-action-btn"
              title="ویرایش"
              onClick={() => openEdit(row)}
            >
              <Icon name="edit" />
            </button>
          )}
          {(!permissionsEnabled || canDelete) && canDeleteRow(row) && (
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
    [actionsIndex, extraRowActions, openEdit, openDelete, reloadTable, permissionsEnabled, canEdit, canDelete, canEditRow, canDeleteRow],
  )

  const formModalTitle = editRow ? `ویرایش ${title}` : (createLabel ?? `${title} جدید`)

  const content = (
    <>
      <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between gap-3 flex-wrap">
        <h2 className="card-title mb-0">{title}</h2>
        <div className="d-flex align-items-center gap-2 flex-wrap">
          {headerExtra}
          {(!permissionsEnabled || canCreate) && (
            <button
              type="button"
              className="btn btn-sm btn-accent btn-users-new d-inline-flex align-items-center gap-2"
              title={createLabel ?? 'ایجاد'}
              onClick={openCreate}
            >
              <Icon name="plus" />
              <span>{createLabel ?? 'ایجاد'}</span>
            </button>
          )}
        </div>
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
    </>
  )

  return (
    <div className="users-page">
      {embedded ? (
        content
      ) : (
        <div className="content-card card border-0 h-100">{content}</div>
      )}

      {(showCreate || editRow) && (
        <>
          <div className="modal-backdrop show users-modal-backdrop" onClick={closeModals} />
          <div
            className="modal show d-block users-modal"
            tabIndex="-1"
            role="dialog"
            data-bs-focus="false"
          >
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable modal-lg">
              <form className="modal-content" ref={formRef} onSubmit={handleSubmit}>
                <div className="modal-header">
                  <h5 className="modal-title">{formModalTitle}</h5>
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
                    {visibleFields.map((field) =>
                      field.type === 'switch' ? (
                        <div className={`col-md-${field.col ?? 12}`} key={field.name}>
                          <div className="form-check form-switch mt-4">
                            <input
                              className="form-check-input"
                              type="checkbox"
                              id={`crud-field-${field.name}`}
                              checked={Boolean(form[field.name])}
                              disabled={field.readOnlyOnEdit && Boolean(editRow)}
                              onChange={(e) =>
                                handleFieldChange(field.name, e.target.checked)
                              }
                            />
                            <label
                              className="form-check-label"
                              htmlFor={`crud-field-${field.name}`}
                            >
                              {field.label}
                            </label>
                          </div>
                        </div>
                      ) : (
                        <div className={`col-md-${field.col ?? 12}`} key={field.name}>
                          <label className="form-label">{field.label}</label>
                          <FieldInput
                            field={field}
                            value={form[field.name]}
                            onChange={handleFieldChange}
                            optionsMap={optionsMap}
                            readOnly={field.readOnlyOnEdit && Boolean(editRow)}
                          />
                        </div>
                      ),
                    )}
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
          <div
            className="modal show d-block users-modal"
            tabIndex="-1"
            role="dialog"
            data-bs-focus="false"
          >
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable">
              <div className="modal-content">
                <div className="modal-header">
                  <h5 className="modal-title">حذف {title}</h5>
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
                    {deleteConfirmText ?? 'آیا از حذف'}{' '}
                    <strong>{deleteRow[nameField]}</strong> اطمینان دارید؟
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

export default CrudTablePage
