import CrudTablePage from '../Transport/CrudTablePage'
import { categoriesApi, fetchCategoryOptions } from '../../services/productsApi'

const columns = [
  { data: 'name', title: 'نام دسته‌بندی' },
  { data: 'parentName', title: 'دسته والد', orderable: false },
  { data: 'description', title: 'توضیحات', orderable: false },
  {
    data: 'productsCount',
    title: 'تعداد محصولات',
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
  {
    name: 'parentCategoryId',
    label: 'دسته والد',
    type: 'select',
    loadOptions: fetchCategoryOptions,
  },
  { name: 'description', label: 'توضیحات', type: 'textarea' },
  { name: 'isActive', label: 'فعال', type: 'switch', default: true },
]

function CategoriesPage() {
  return (
    <CrudTablePage
      title="دسته‌بندی محصولات"
      createLabel="دسته جدید"
      api={categoriesApi}
      idField="categoryId"
      nameField="name"
      columns={columns}
      fields={fields}
      permissionPath="/products/categories"
    />
  )
}

export default CategoriesPage
