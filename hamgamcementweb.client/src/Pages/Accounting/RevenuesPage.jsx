import CrudTablePage, { formatJalaliDate } from '../Transport/CrudTablePage'
import { fetchCurrencyOptions } from '../../services/transportApi'
import { fetchCustomerOptions } from '../../services/transactionsApi'
import { revenuesApi, fetchAccountingRevenueCategoryOptions } from '../../services/financeApi'

const columns = [
  { data: 'title', title: 'عنوان' },
  {
    data: 'revenueDate',
    title: 'تاریخ',
    render: (data) => formatJalaliDate(data),
  },
  { data: 'categoryName', title: 'دسته‌بندی' },
  { data: 'sourceLabel', title: 'منبع', orderable: false },
  { data: 'customerName', title: 'مشتری', orderable: false },
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
  },
  {
    data: 'invoiceNumber',
    title: 'فاکتور',
    orderable: false,
    render: (data) => (data ? `<span class="badge bg-secondary">${data}</span>` : '—'),
  },
]

const fields = [
  { name: 'title', label: 'عنوان', type: 'text', required: true, col: 6 },
  {
    name: 'revenueDate',
    label: 'تاریخ',
    type: 'jalali-date',
    required: true,
    col: 6,
    default: new Date().toISOString().slice(0, 10),
  },
  {
    name: 'revenueCategoryId',
    label: 'دسته‌بندی',
    type: 'select',
    required: true,
    col: 6,
    loadOptions: fetchAccountingRevenueCategoryOptions,
  },
  {
    name: 'customerId',
    label: 'مشتری (اختیاری)',
    type: 'select',
    col: 6,
    loadOptions: fetchCustomerOptions,
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
  { name: 'description', label: 'توضیحات', type: 'textarea', col: 12 },
]

function RevenuesPage() {
  return (
    <CrudTablePage
      title="عواید"
      createLabel="عاید متفرقه"
      api={revenuesApi}
      idField="revenueId"
      nameField="title"
      columns={columns}
      fields={fields}
      defaultOrder={[[2, 'desc']]}
      permissionPath="/accounting/revenues"
      canEditRow={(row) => !row.isFromInvoice}
      canDeleteRow={(row) => !row.isFromInvoice}
      deleteConfirmText="آیا از حذف این عاید اطمینان دارید؟"
    />
  )
}

export default RevenuesPage
