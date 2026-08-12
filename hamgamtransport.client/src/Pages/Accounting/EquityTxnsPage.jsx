import { useCallback } from 'react'
import CrudTablePage, { formatJalaliDate } from '../../components/common/CrudTablePage'
import { formatAmount } from '../../lib/dataTableOptions'
import { fetchCurrencyOptions } from '../../services/currencyApi'
import {
  equityTxnsApi,
  fetchDistributable,
  fetchEquityCashBoxOptions,
  fetchShareholderOptions,
} from '../../services/equityApi'

const TXN_TYPE_OPTIONS = [
  { value: 1, label: 'آورده سرمایه' },
  { value: 2, label: 'برداشت سرمایه' },
  { value: 3, label: 'توزیع سود' },
]

const SETTLEMENT_OPTIONS = [
  { value: 1, label: 'نقدی (صندوق)' },
  { value: 2, label: 'بدهی (پرداختنی)' },
]

const columns = [
  {
    data: 'txnDate',
    title: 'تاریخ',
    render: (data) => formatJalaliDate(data),
  },
  { data: 'shareholderName', title: 'سهام‌دار', orderable: false },
  { data: 'txnTypeLabel', title: 'نوع', orderable: false },
  {
    data: 'amount',
    title: 'مبلغ',
    format: 'amount',
    className: 'text-end',
  },
  {
    data: 'amountInBaseCurrency',
    title: 'مبلغ (ارز پایه)',
    format: 'amount',
    className: 'text-end',
    orderable: false,
  },
  {
    data: 'profitPortionInBase',
    title: 'از سود',
    format: 'amount',
    className: 'text-end',
    orderable: false,
    render: (data, _type, row) => {
      if (Number(row.txnType) !== 3) return '—'
      return formatAmount(data ?? 0)
    },
  },
  {
    data: 'capitalPortionInBase',
    title: 'از سرمایه',
    format: 'amount',
    className: 'text-end',
    orderable: false,
    render: (data, _type, row) => {
      if (Number(row.txnType) !== 3) return '—'
      return formatAmount(data ?? 0)
    },
  },
  { data: 'settlementModeLabel', title: 'تسویه', orderable: false },
  {
    data: 'description',
    title: 'توضیحات',
    orderable: false,
    render: (data) => data || '—',
  },
]

const fields = [
  {
    name: 'txnType',
    label: 'نوع سند',
    type: 'select',
    required: true,
    col: 6,
    options: TXN_TYPE_OPTIONS,
    default: 1,
  },
  {
    name: 'shareholderId',
    label: 'سهام‌دار',
    type: 'select',
    required: true,
    col: 6,
    loadOptions: fetchShareholderOptions,
  },
  {
    name: 'txnDate',
    label: 'تاریخ',
    type: 'jalali-date',
    required: true,
    col: 6,
    default: new Date().toISOString().slice(0, 10),
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
    name: 'amount',
    label: 'مبلغ',
    type: 'number',
    required: true,
    col: 6,
  },
  {
    name: 'settlementMode',
    label: 'نحوه تسویه (توزیع سود)',
    type: 'select',
    col: 6,
    options: SETTLEMENT_OPTIONS,
    default: 1,
    showWhen: (form) => Number(form.txnType) === 3,
  },
  {
    name: 'cashBoxId',
    label: 'صندوق (تسویه نقدی)',
    type: 'select',
    col: 6,
    loadOptions: fetchEquityCashBoxOptions,
    showWhen: (form) =>
      Number(form.txnType) !== 3 || Number(form.settlementMode) !== 2,
  },
  {
    name: 'distributableHint',
    label: 'سهم سود قابل‌برداشت',
    type: 'readonly',
    col: 12,
    omitFromPayload: true,
    default: '',
    showWhen: (form) => Number(form.txnType) === 3,
  },
  {
    name: 'splitWarning',
    label: 'هشدار تفکیک',
    type: 'readonly',
    col: 12,
    omitFromPayload: true,
    default: '',
    showWhen: (form) =>
      Number(form.txnType) === 3 && Boolean(form.splitWarning),
  },
  { name: 'description', label: 'توضیحات', type: 'textarea', col: 12 },
]

function buildSplitWarning(amount, availableInBase) {
  const num = Number(amount)
  if (!Number.isFinite(num) || num <= 0) return ''
  if (!Number.isFinite(availableInBase)) return ''
  return `سقف سهم سود حدود ${formatAmount(availableInBase)} (ارز پایه) است؛ اگر معادل پایهٔ مبلغ بیشتر باشد، مازاد از سرمایهٔ همین سهام‌دار کسر می‌شود.`
}

function EquityTxnsPage() {
  const handleFormChange = useCallback(async (name, _value, next) => {
    if (Number(next.txnType) !== 3) {
      return { distributableHint: '', splitWarning: '', _availableInBase: null }
    }

    if (name === 'amount' && next._availableInBase != null) {
      return {
        splitWarning: buildSplitWarning(next.amount, Number(next._availableInBase)),
      }
    }

    if (
      name !== 'txnType' &&
      name !== 'shareholderId' &&
      name !== 'txnDate' &&
      name !== 'amount'
    ) {
      return null
    }

    const shareholderId = Number(next.shareholderId)
    if (!Number.isFinite(shareholderId) || shareholderId <= 0) {
      return {
        distributableHint: 'سهام‌دار را انتخاب کنید تا سقف سهم سود محاسبه شود.',
        splitWarning: '',
        _availableInBase: null,
      }
    }

    const asOf = next.txnDate || new Date().toISOString().slice(0, 10)
    try {
      const data = await fetchDistributable(shareholderId, asOf)
      const available = Number(data?.availableInBase ?? 0)
      const hint = `تا این تاریخ حدود ${formatAmount(available)} (ارز پایه) از سهم سود قابل‌برداشت است — سهم ${formatAmount(data?.profitSharePercent ?? 0)}٪ از سود سال جاری.`
      return {
        distributableHint: hint,
        splitWarning: buildSplitWarning(next.amount, available),
        _availableInBase: available,
      }
    } catch (error) {
      return {
        distributableHint: error.message || 'محاسبه سهم قابل‌برداشت ممکن نشد.',
        splitWarning: '',
        _availableInBase: null,
      }
    }
  }, [])

  return (
    <CrudTablePage
      title="حقوق صاحبان سهام"
      createLabel="سند سرمایه جدید"
      api={equityTxnsApi}
      idField="shareholderEquityTxnId"
      nameField="txnTypeLabel"
      columns={columns}
      fields={fields}
      defaultOrder={[[1, 'desc']]}
      permissionPath="/accounting/equity"
      canEditRow={() => false}
      onFormChange={handleFormChange}
      deleteConfirmText="آیا از حذف این سند سرمایه اطمینان دارید؟ سند دفتر نیز باطل می‌شود."
    />
  )
}

export default EquityTxnsPage
