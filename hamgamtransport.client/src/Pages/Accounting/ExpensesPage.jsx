import CrudTablePage, { formatJalaliDate } from '../../components/common/CrudTablePage'
import { fetchCurrencyOptions } from '../../services/currencyApi'
import { fetchSupplierOptions } from '../../services/transactionsApi'
import { expensesApi, fetchAccountingExpenseCategoryOptions } from '../../services/financeApi'

const columns = [
  { data: 'title', title: 'عنوان' },
  {
    data: 'expenseDate',
    title: 'تاریخ',
    render: (data) => formatJalaliDate(data),
  },
  { data: 'categoryName', title: 'دسته‌بندی' },
  { data: 'sourceLabel', title: 'منبع', orderable: false },
  { data: 'supplierName', title: 'تأمین‌کننده', orderable: false },
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
    name: 'expenseDate',
    label: 'تاریخ',
    type: 'jalali-date',
    required: true,
    col: 6,
    default: new Date().toISOString().slice(0, 10),
  },
  {
    name: 'expenseCategoryId',
    label: 'دسته‌بندی',
    type: 'select',
    required: true,
    col: 6,
    loadOptions: fetchAccountingExpenseCategoryOptions,
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
    name: 'amount',
    label: 'مبلغ',
    type: 'number',
    required: true,
    col: 6,
  },
  { name: 'description', label: 'توضیحات', type: 'textarea', col: 12 },
]

function ExpensesPage() {
  return (
    <CrudTablePage
      title="مصارف"
      createLabel="مصرف متفرقه"
      api={expensesApi}
      idField="expenseId"
      nameField="title"
      columns={columns}
      fields={fields}
      defaultOrder={[[2, 'desc']]}
      permissionPath="/accounting/expenses"
      canEditRow={(row) => !row.isFromInvoice}
      canDeleteRow={(row) => !row.isFromInvoice}
      deleteConfirmText="آیا از حذف این مصرف اطمینان دارید؟"
    />
  )
}

export default ExpensesPage
