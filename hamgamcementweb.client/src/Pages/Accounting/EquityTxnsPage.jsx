import CrudTablePage, { formatJalaliDate } from '../Transport/CrudTablePage'
import { fetchCurrencyOptions } from '../../services/transportApi'
import {
  equityTxnsApi,
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
  { name: 'description', label: 'توضیحات', type: 'textarea', col: 12 },
]

function EquityTxnsPage() {
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
      deleteConfirmText="آیا از حذف این سند سرمایه اطمینان دارید؟ سند دفتر نیز باطل می‌شود."
    />
  )
}

export default EquityTxnsPage
