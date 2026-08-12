import CrudTablePage from '../../components/common/CrudTablePage'
import { vehicleOwnersApi, vehicleTypesApi, vehiclesApi } from '../../services/transportApi'

const roleOptions = [
  { value: 1, label: 'کشنده' },
  { value: 2, label: 'بونکر' },
  { value: 3, label: 'تک‌وسیله' },
]

const columns = [
  { data: 'plateNumber', title: 'پلاک' },
  { data: 'typeName', title: 'نوع' },
  { data: 'ownerName', title: 'مالک' },
  {
    data: 'roleInPair',
    title: 'نقش',
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
  { name: 'plateNumber', label: 'پلاک', type: 'text', required: true, col: 4 },
  {
    name: 'vehicleTypeId',
    label: 'نوع وسیله',
    type: 'select',
    required: true,
    col: 4,
    loadOptions: () => vehicleTypesApi.options(),
  },
  {
    name: 'vehicleOwnerId',
    label: 'مالک',
    type: 'select',
    required: true,
    col: 4,
    loadOptions: () => vehicleOwnersApi.options(),
  },
  {
    name: 'roleInPair',
    label: 'نقش',
    type: 'select',
    col: 4,
    default: 1,
    options: roleOptions,
  },
  { name: 'isActive', label: 'فعال', type: 'switch', default: true, col: 4 },
]

export default function VehiclesPage() {
  return (
    <CrudTablePage
      title="کشنده و بونکر"
      createLabel="وسیله جدید"
      api={vehiclesApi}
      idField="vehicleId"
      nameField="plateNumber"
      columns={columns}
      fields={fields}
      permissionPath="/transport/vehicles"
    />
  )
}
