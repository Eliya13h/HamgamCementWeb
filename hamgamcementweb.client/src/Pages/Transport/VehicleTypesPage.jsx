import CrudTablePage from './CrudTablePage'
import { vehicleTypesApi } from '../../services/transportApi'

const columns = [
  { data: 'name', title: 'نام نوع' },
  { data: 'description', title: 'توضیحات', orderable: false },
  {
    data: 'vehiclesCount',
    title: 'تعداد وسایل',
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
  { name: 'name', label: 'نام نوع وسیله نقلیه', type: 'text', required: true },
  { name: 'description', label: 'توضیحات', type: 'textarea' },
  { name: 'isActive', label: 'فعال', type: 'switch', default: true },
]

function VehicleTypesPage() {
  return (
    <CrudTablePage
      title="انواع وسایل نقلیه"
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

export default VehicleTypesPage
