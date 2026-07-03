import CrudTablePage from './CrudTablePage'
import { expenseCategoriesApi } from '../../services/transportApi'

const columns = [
  { data: 'name', title: 'نام دسته‌بندی' },
  { data: 'description', title: 'توضیحات', orderable: false },
  {
    data: 'expensesCount',
    title: 'تعداد مصارف',
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

function ExpenseCategoriesPage() {
  return (
    <CrudTablePage
      title="دسته‌بندی مصارف"
      createLabel="دسته‌بندی جدید"
      api={expenseCategoriesApi}
      idField="expensesCategoryId"
      nameField="name"
      columns={columns}
      fields={fields}
      permissionPath="/transport/expense-categories"
    />
  )
}

export default ExpenseCategoriesPage
