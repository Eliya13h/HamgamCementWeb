import CrudTablePage from '../../components/common/CrudTablePage'
import { vehicleOwnersApi } from '../../services/transportApi'

const columns = [
  { data: 'name', title: 'نام' },
  { data: 'phoneNumber', title: 'تلفن', defaultContent: '' },
  { data: 'city', title: 'شهر', defaultContent: '' },
  {
    data: 'ownerType',
    title: 'نوع',
    render: (v) => (Number(v) === 2 ? 'حقوقی' : 'حقیقی'),
  },
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
  { name: 'title', label: 'عنوان', type: 'select', col: 3, default: 1, options: [{ value: 1, label: 'آقا' }, { value: 2, label: 'خانم' }] },
  { name: 'name', label: 'نام', type: 'text', required: true, col: 9 },
  { name: 'phoneNumber', label: 'تلفن', type: 'text', col: 4 },
  { name: 'city', label: 'شهر', type: 'text', col: 4 },
  {
    name: 'ownerType',
    label: 'نوع',
    type: 'select',
    col: 4,
    default: 1,
    options: [
      { value: 1, label: 'حقیقی' },
      { value: 2, label: 'حقوقی' },
    ],
  },
  { name: 'address', label: 'آدرس', type: 'textarea', col: 12 },
  { name: 'initialBalance', label: 'مانده اولیه', type: 'number', col: 4, default: 0 },
  { name: 'isActive', label: 'فعال', type: 'switch', default: true, col: 4 },
]

export default function VehicleOwnersPage() {
  return (
    <CrudTablePage
      title="مالکان وسیله"
      createLabel="مالک جدید"
      api={vehicleOwnersApi}
      idField="vehicleOwnerId"
      nameField="name"
      columns={columns}
      fields={fields}
      permissionPath="/people/vehicle-owners"
    />
  )
}
