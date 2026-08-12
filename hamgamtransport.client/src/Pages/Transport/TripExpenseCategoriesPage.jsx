import CrudTablePage from '../../components/common/CrudTablePage'
import { tripExpenseCategoriesApi } from '../../services/transportApi'

const columns = [
  { data: 'code', title: 'کد' },
  { data: 'name', title: 'نام' },
  {
    data: 'isActive',
    title: 'وضعیت',
    className: 'text-center',
    render: (v) =>
      v
        ? '<span class="badge badge-active">فعال</span>'
        : '<span class="badge badge-inactive">غیرفعال</span>',
  },
]

const fields = [
  { name: 'code', label: 'کد', type: 'text', required: true, col: 4 },
  { name: 'name', label: 'نام', type: 'text', required: true, col: 8 },
  { name: 'isActive', label: 'فعال', type: 'switch', default: true, col: 4 },
]

export default function TripExpenseCategoriesPage() {
  return (
    <CrudTablePage
      title="دسته‌بندی هزینه سفر"
      createLabel="دسته جدید"
      api={tripExpenseCategoriesApi}
      idField="tripExpenseCategoryId"
      nameField="name"
      columns={columns}
      fields={fields}
      permissionPath="/transport/trip-expense-categories"
    />
  )
}
