import CrudTablePage from '../../components/common/CrudTablePage'
import { vehiclePairsApi, vehiclesApi } from '../../services/transportApi'

const columns = [
  { data: 'code', title: 'کد' },
  { data: 'name', title: 'نام' },
  { data: 'primaryPlate', title: 'کشنده', defaultContent: '—' },
  { data: 'secondaryPlate', title: 'بونکر', defaultContent: '—' },
  { data: 'primarySharePercent', title: '٪ کشنده' },
  { data: 'secondarySharePercent', title: '٪ بونکر' },
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
    name: 'primaryVehicleId',
    label: 'کشنده',
    type: 'select',
    col: 6,
    loadOptions: () => vehiclesApi.options(1),
  },
  {
    name: 'secondaryVehicleId',
    label: 'بونکر',
    type: 'select',
    col: 6,
    loadOptions: () => vehiclesApi.options(2),
  },
  { name: 'primarySharePercent', label: 'سهم کشنده (٪)', type: 'number', col: 4, default: 60 },
  { name: 'secondarySharePercent', label: 'سهم بونکر (٪)', type: 'number', col: 4, default: 40 },
  { name: 'isActive', label: 'فعال', type: 'switch', default: true, col: 4 },
]

export default function VehiclePairsPage() {
  return (
    <CrudTablePage
      title="جفت کشنده/بونکر"
      createLabel="جفت جدید"
      api={vehiclePairsApi}
      idField="vehiclePairId"
      nameField="name"
      columns={columns}
      fields={fields}
      permissionPath="/transport/vehicle-pairs"
    />
  )
}
