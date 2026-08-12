import CrudTablePage from '../../components/common/CrudTablePage'
import { vehicleTypesApi } from '../../services/transportApi'

const roleOptions = [
  { value: 1, label: 'کشنده' },
  { value: 2, label: 'بونکر' },
  { value: 3, label: 'تک‌وسیله' },
]

const columns = [
  { data: 'code', title: 'کد' },
  { data: 'name', title: 'نام' },
  {
    data: 'defaultRole',
    title: 'نقش پیش‌فرض',
    render: (v) => roleOptions.find((r) => r.value === Number(v))?.label ?? '—',
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
  { name: 'code', label: 'کد', type: 'text', required: true, col: 4 },
  { name: 'name', label: 'نام', type: 'text', required: true, col: 8 },
  {
    name: 'defaultRole',
    label: 'نقش پیش‌فرض',
    type: 'select',
    col: 6,
    default: 1,
    options: roleOptions,
  },
  { name: 'isActive', label: 'فعال', type: 'switch', default: true, col: 4 },
]

export default function VehicleTypesPage() {
  return (
    <CrudTablePage
      title="انواع وسیله"
      createLabel="نوع جدید"
      api={vehicleTypesApi}
      idField="vehicleTypeId"
      nameField="name"
      columns={columns}
      fields={fields}
      permissionPath="/transport/vehicle-types"
    />
  )
}
