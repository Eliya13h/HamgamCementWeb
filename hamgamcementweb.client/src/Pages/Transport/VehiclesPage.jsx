import CrudTablePage from './CrudTablePage'
import {
  fetchDriverOptions,
  fetchVehicleOwnerOptions,
  fetchVehicleTypeOptions,
  vehiclesApi,
} from '../../services/transportApi'

const columns = [
  { data: 'code', title: 'کد' },
  { data: 'plateNumber', title: 'پلاک' },
  { data: 'vehicleTypeName', title: 'نوع', orderable: false },
  { data: 'defaultDriverName', title: 'راننده', orderable: false, render: (d) => d ?? '—' },
  { data: 'vehicleOwnerName', title: 'صاحب ماشین', orderable: false, render: (d) => d ?? '—' },
  { data: 'brand', title: 'برند/مدل' },
  {
    data: 'modelYear',
    title: 'سال ساخت',
    className: 'text-center',
    render: (data) => data ?? '—',
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
  {
    name: 'code',
    label: 'کد وسیله نقلیه',
    type: 'readonly',
    showOnlyOnEdit: true,
    autoCode: true,
    col: 6,
  },
  { name: 'plateNumber', label: 'شماره پلاک', type: 'text', required: true, col: 6 },
  {
    name: 'vehicleTypeId',
    label: 'نوع وسیله نقلیه',
    type: 'select',
    required: true,
    col: 6,
    loadOptions: fetchVehicleTypeOptions,
  },
  {
    name: 'defaultDriverId',
    label: 'راننده پیش‌فرض',
    type: 'select',
    col: 6,
    loadOptions: fetchDriverOptions,
  },
  {
    name: 'vehicleOwnerId',
    label: 'صاحب ماشین',
    type: 'select',
    col: 6,
    loadOptions: fetchVehicleOwnerOptions,
  },
  { name: 'brand', label: 'برند / مدل', type: 'text', col: 6 },
  { name: 'modelYear', label: 'سال ساخت', type: 'number', step: '1', col: 4 },
  { name: 'color', label: 'رنگ', type: 'text', col: 4 },
  { name: 'fuelTankCapacity', label: 'ظرفیت تانک سوخت (لیتر)', type: 'number', col: 4 },
  { name: 'chassisNumber', label: 'شماره شاسی', type: 'text', col: 6 },
  { name: 'engineNumber', label: 'شماره موتور', type: 'text', col: 6 },
  { name: 'description', label: 'توضیحات', type: 'textarea' },
  { name: 'isActive', label: 'فعال', type: 'switch', default: true },
]

function VehiclesPage() {
  return (
    <CrudTablePage
      title="وسایل نقلیه"
      createLabel="وسیله نقلیه جدید"
      api={vehiclesApi}
      idField="vehicleId"
      nameField="code"
      columns={columns}
      fields={fields}
      permissionPath="/transport/vehicles"
    />
  )
}

export default VehiclesPage
