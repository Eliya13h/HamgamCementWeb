import CrudTablePage from '../Transport/CrudTablePage'
import { fetchCurrencyOptions } from '../../services/transportApi'
import { bankAccountsApi } from '../../services/ledgerApi'

const columns = [
  { data: 'code', title: 'کد' },
  { data: 'name', title: 'نام' },
  { data: 'accountNumber', title: 'شماره حساب', orderable: false },
  { data: 'currencyCode', title: 'ارز', orderable: false },
  {
    data: 'isActive',
    title: 'وضعیت',
    className: 'text-center',
    render: (data) =>
      data
        ? '<span class="badge badge-active">فعال</span>'
        : '<span class="badge badge-inactive">غیرفعال</span>',
  },
]

const fields = [
  {
    name: 'code',
    label: 'کد (خودکار)',
    type: 'text',
    col: 4,
    showOnlyOnEdit: true,
    readOnlyOnEdit: true,
  },
  { name: 'name', label: 'نام', type: 'text', required: true, col: 4 },
  {
    name: 'accountNumber',
    label: 'شماره حساب',
    type: 'text',
    col: 4,
  },
  {
    name: 'currencyId',
    label: 'ارز',
    type: 'select',
    col: 4,
    loadOptions: fetchCurrencyOptions,
  },
  { name: 'description', label: 'توضیحات', type: 'textarea', col: 12 },
  { name: 'isActive', label: 'فعال', type: 'switch', col: 4, default: true },
]

const bankAccountsPageApi = {
  createDataTableAjax: bankAccountsApi.createDataTableAjax,
  create: (payload) =>
    bankAccountsApi.create({
      name: payload.name,
      accountNumber: payload.accountNumber,
      currencyId: payload.currencyId,
      description: payload.description,
      isActive: payload.isActive,
    }),
  update: (id, payload) =>
    bankAccountsApi.update(id, {
      name: payload.name,
      accountNumber: payload.accountNumber,
      currencyId: payload.currencyId,
      description: payload.description,
      isActive: payload.isActive,
    }),
  get: bankAccountsApi.get,
  remove: () =>
    Promise.reject(new Error('حذف حساب بانکی از این صفحه پشتیبانی نمی‌شود.')),
}

function BankAccountsPage() {
  return (
    <CrudTablePage
      title="حساب‌های بانکی"
      createLabel="حساب بانکی جدید"
      api={bankAccountsPageApi}
      idField="bankAccountId"
      nameField="name"
      columns={columns}
      fields={fields}
      permissionPath="/cash/banks"
      canDeleteRow={() => false}
    />
  )
}

export default BankAccountsPage
