import CrudTablePage from '../Transport/CrudTablePage'
import { revenueCategoriesApi } from '../../services/financeApi'

const columns = [
  { data: 'name', title: 'نام دسته‌بندی' },
  { data: 'description', title: 'توضیحات', orderable: false },
  {
    data: 'isSystem',
    title: 'نوع',
    orderable: false,
    className: 'text-center',
    render: (data) =>
      data
        ? '<span class="badge bg-secondary">سیستمی</span>'
        : '<span class="badge bg-light text-dark">کاربری</span>',
  },
  {
    data: 'revenuesCount',
    title: 'تعداد عواید',
    orderable: false,
    className: 'text-center',
  },
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
  { name: 'name', label: 'نام دسته‌بندی', type: 'text', required: true },
  { name: 'description', label: 'توضیحات', type: 'textarea' },
  { name: 'isActive', label: 'فعال', type: 'switch', default: true },
]

function RevenueCategoriesPage() {
  return (
    <CrudTablePage
      title="دسته‌بندی عواید"
      createLabel="دسته‌بندی جدید"
      api={revenueCategoriesApi}
      idField="revenueCategoryId"
      nameField="name"
      columns={columns}
      fields={fields}
      permissionPath="/accounting/revenue-categories"
      canDeleteRow={(row) => !row.isSystem}
      canEditRow={(row) => !row.isSystem || row.name === 'متفرقه'}
    />
  )
}

export default RevenueCategoriesPage
