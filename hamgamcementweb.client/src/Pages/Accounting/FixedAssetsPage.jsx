import { useCallback, useRef, useState } from 'react'
import {
  useModalKeyboardShortcuts,
  useModalAutoFocus,
} from '../../hooks/useModalKeyboardShortcuts'
import { showAppToast } from '../../lib/appToast'
import { persianValidity, validateFormPersian } from '../../lib/persianFormValidity'
import CrudTablePage, { formatJalaliDate } from '../Transport/CrudTablePage'
import { fetchCurrencyOptions } from '../../services/transportApi'
import { fetchSupplierOptions } from '../../services/transactionsApi'
import {
  fixedAssetsApi,
  fetchFixedAssetCategoryOptions,
} from '../../services/financeApi'

const columns = [
  { data: 'code', title: 'کد' },
  { data: 'name', title: 'نام دارایی' },
  { data: 'categoryName', title: 'دسته', orderable: false },
  {
    data: 'acquisitionDate',
    title: 'تاریخ خرید',
    render: (data) => formatJalaliDate(data),
  },
  {
    data: 'costAmountInBaseCurrency',
    title: 'بهای تمام‌شده',
    format: 'amount',
    className: 'text-end',
  },
  {
    data: 'accumulatedDepreciationInBaseCurrency',
    title: 'استهلاک انباشته',
    format: 'amount',
    className: 'text-end',
    orderable: false,
  },
  {
    data: 'bookValueInBaseCurrency',
    title: 'ارزش دفتری',
    format: 'amount',
    className: 'text-end',
    orderable: false,
  },
  {
    data: 'statusLabel',
    title: 'وضعیت',
    orderable: false,
    className: 'text-center',
  },
]

const fields = [
  {
    name: 'code',
    label: 'کد (اختیاری)',
    type: 'text',
    col: 4,
    placeholder: 'خالی = تولید خودکار',
  },
  { name: 'name', label: 'نام دارایی', type: 'text', required: true, col: 8 },
  {
    name: 'fixedAssetCategoryId',
    label: 'دسته‌بندی',
    type: 'select',
    required: true,
    col: 6,
    loadOptions: async () => {
      const rows = await fetchFixedAssetCategoryOptions()
      return rows.map((r) => ({
        value: String(r.value),
        label: r.label,
        defaultUsefulLifeMonths: r.defaultUsefulLifeMonths,
      }))
    },
  },
  {
    name: 'acquisitionDate',
    label: 'تاریخ خرید',
    type: 'jalali-date',
    required: true,
    col: 6,
    default: new Date().toISOString().slice(0, 10),
  },
  {
    name: 'supplierId',
    label: 'تأمین‌کننده (اختیاری)',
    type: 'select',
    col: 6,
    loadOptions: fetchSupplierOptions,
  },
  {
    name: 'currencyId',
    label: 'ارز',
    type: 'select',
    required: true,
    col: 6,
    loadOptions: fetchCurrencyOptions,
  },
  {
    name: 'costAmount',
    label: 'بهای تمام‌شده',
    type: 'number',
    required: true,
    col: 4,
  },
  {
    name: 'salvageValue',
    label: 'ارزش اسقاط',
    type: 'number',
    col: 4,
    default: 0,
  },
  {
    name: 'usefulLifeMonths',
    label: 'عمر مفید (ماه)',
    type: 'number',
    required: true,
    col: 4,
    default: 60,
  },
  { name: 'description', label: 'توضیحات', type: 'textarea', col: 12 },
]

function FixedAssetsPage() {
  const disposeFormRef = useRef(null)
  const [busy, setBusy] = useState(false)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  const [disposeRow, setDisposeRow] = useState(null)
  const [disposeAmount, setDisposeAmount] = useState('0')
  const [disposeDate, setDisposeDate] = useState(
    () => new Date().toISOString().slice(0, 10),
  )
  const [reloadKey, setReloadKey] = useState(0)

  const closeDispose = useCallback(() => {
    setDisposeRow(null)
  }, [])

  const runDepreciation = useCallback(async () => {
    if (busy) return
    if (!window.confirm('استهلاک ماه جاری برای همه دارایی‌های فعال اجرا شود؟')) {
      return
    }
    setBusy(true)
    setError('')
    setMessage('')
    try {
      const result = await fixedAssetsApi.depreciatePeriod({})
      setMessage(result.message ?? 'استهلاک انجام شد.')
      setReloadKey((k) => k + 1)
    } catch (err) {
      setError(err.message)
    } finally {
      setBusy(false)
    }
  }, [busy])

  const submitDispose = useCallback(
    async (event) => {
      event?.preventDefault?.()
      if (!disposeRow || busy) return

      const formEl = disposeFormRef.current
      if (formEl) {
        const validationMessage = validateFormPersian(formEl)
        if (validationMessage) {
          showAppToast(validationMessage)
          formEl.reportValidity()
          return
        }
      }

      setBusy(true)
      setError('')
      setMessage('')
      try {
        const result = await fixedAssetsApi.dispose(disposeRow.fixedAssetId, {
          disposalDate: disposeDate,
          disposalAmount: Number(disposeAmount) || 0,
        })
        setMessage(result.message ?? 'فروش/اسقاط ثبت شد.')
        setDisposeRow(null)
        setReloadKey((k) => k + 1)
      } catch (err) {
        setError(err.message)
      } finally {
        setBusy(false)
      }
    },
    [busy, disposeRow, disposeAmount, disposeDate],
  )

  const triggerDisposeSave = useCallback(() => {
    if (!busy) disposeFormRef.current?.requestSubmit()
  }, [busy])

  useModalKeyboardShortcuts({
    open: Boolean(disposeRow),
    onClose: closeDispose,
    onSave: triggerDisposeSave,
    formRef: disposeFormRef,
  })

  useModalAutoFocus({ open: Boolean(disposeRow), formRef: disposeFormRef })

  return (
    <div className="users-page">
      <div className="content-card card border-0">
        {(message || error) && (
          <div className="px-4 pt-3">
            {message && <div className="alert alert-success py-2 mb-0">{message}</div>}
            {error && <div className="alert alert-danger py-2 mb-0">{error}</div>}
          </div>
        )}

        <div className="px-4 pt-3 d-flex justify-content-end">
          <button
            type="button"
            className="btn btn-sm btn-outline-primary"
            disabled={busy}
            onClick={runDepreciation}
          >
            اجرای استهلاک ماه جاری
          </button>
        </div>

        <CrudTablePage
          key={reloadKey}
          embedded
          title="دارایی‌های ثابت"
          createLabel="ثبت دارایی"
          api={fixedAssetsApi}
          idField="fixedAssetId"
          nameField="name"
          columns={columns}
          fields={fields}
          defaultOrder={[[4, 'desc']]}
          permissionPath="/accounting/fixed-assets"
          canEditRow={(row) => row.status !== 3}
          canDeleteRow={(row) =>
            row.status !== 3 && Number(row.accumulatedDepreciationInBaseCurrency) <= 0
          }
          deleteConfirmText="آیا از حذف این دارایی و سند خرید آن اطمینان دارید؟"
          extraRowActions={(row) =>
            row.canDispose ? (
              <button
                type="button"
                className="dt-action-btn"
                title="فروش / اسقاط"
                onClick={() => {
                  setDisposeRow(row)
                  setDisposeAmount('0')
                  setDisposeDate(new Date().toISOString().slice(0, 10))
                }}
              >
                فروش
              </button>
            ) : null
          }
        />
      </div>

      {disposeRow && (
        <>
          <div
            className="modal-backdrop show users-modal-backdrop"
            onClick={closeDispose}
          />
          <div
            className="modal show d-block users-modal"
            tabIndex={-1}
            role="dialog"
            data-bs-focus="false"
          >
            <div className="modal-dialog modal-dialog-centered">
              <form
                ref={disposeFormRef}
                className="modal-content"
                onSubmit={submitDispose}
                noValidate
              >
                <div className="modal-header">
                  <h5 className="modal-title">فروش / اسقاط — {disposeRow.name}</h5>
                  <button
                    type="button"
                    className="btn-close"
                    aria-label="بستن"
                    onClick={closeDispose}
                  />
                </div>
                <div className="modal-body">
                  <div className="mb-3">
                    <label className="form-label">تاریخ</label>
                    <input
                      type="date"
                      className="form-control"
                      value={disposeDate}
                      required
                      {...persianValidity('لطفاً تاریخ را وارد کنید.')}
                      onChange={(e) => setDisposeDate(e.target.value)}
                    />
                  </div>
                  <div className="mb-0">
                    <label className="form-label">مبلغ فروش (۰ = اسقاط بدون درآمد)</label>
                    <input
                      type="number"
                      className="form-control"
                      min="0"
                      step="any"
                      value={disposeAmount}
                      required
                      {...persianValidity('لطفاً مبلغ فروش را وارد کنید.')}
                      onChange={(e) => setDisposeAmount(e.target.value)}
                    />
                  </div>
                </div>
                <div className="modal-footer">
                  <button
                    type="button"
                    className="btn btn-light"
                    onClick={closeDispose}
                  >
                    انصراف
                  </button>
                  <button
                    type="submit"
                    className="btn btn-accent"
                    disabled={busy}
                  >
                    ثبت
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

export default FixedAssetsPage
