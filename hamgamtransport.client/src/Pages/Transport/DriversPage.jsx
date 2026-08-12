import CrudTablePage from '../../components/common/CrudTablePage'
import { driversApi, vehicleOwnersApi } from '../../services/transportApi'

const columns = [
  { data: 'name', title: 'نام' },
  { data: 'phoneNumber', title: 'تلفن', defaultContent: '' },
  { data: 'licenseNumber', title: 'گواهینامه', defaultContent: '' },
  { data: 'ownerName', title: 'مالک', defaultContent: '—' },
  { data: 'defaultProfitSharePercent', title: '٪ سود پیش‌فرض', defaultContent: '—' },
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
  { name: 'name', label: 'نام', type: 'text', required: true, col: 6 },
  { name: 'phoneNumber', label: 'تلفن', type: 'text', col: 6 },
  { name: 'licenseNumber', label: 'شماره گواهینامه', type: 'text', col: 6 },
  {
    name: 'vehicleOwnerId',
    label: 'مالک وسیله',
    type: 'select',
    col: 6,
    loadOptions: () => vehicleOwnersApi.options(),
  },
  { name: 'defaultProfitSharePercent', label: 'درصد سود پیش‌فرض', type: 'number', col: 6 },
  { name: 'isActive', label: 'فعال', type: 'switch', default: true, col: 4 },
]

export default function DriversPage() {
  return (
    <CrudTablePage
      title="رانندگان"
      createLabel="راننده جدید"
      api={driversApi}
      idField="driverId"
      nameField="name"
      columns={columns}
      fields={fields}
      permissionPath="/people/drivers"
    />
  )
}
