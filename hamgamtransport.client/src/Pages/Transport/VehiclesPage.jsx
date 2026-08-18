import { useCallback } from 'react'
import CrudTablePage from '../../components/common/CrudTablePage'
import { driversApi, vehicleOwnersApi, vehicleTypesApi, vehiclesApi } from '../../services/transportApi'

let vehicleTypeOptions = []
vehicleTypesApi.options().then((opts) => {
  vehicleTypeOptions = opts ?? []
})

function resolveTypeCode(vehicleTypeId, fallbackCode = '') {
  if (fallbackCode) return fallbackCode
  const match = vehicleTypeOptions.find((t) => String(t.value) === String(vehicleTypeId))
  return match?.code ?? ''
}

const columns = [
  { data: 'code', title: 'کد' },
  { data: 'plateNumber', title: 'پلاک' },
  { data: 'typeName', title: 'نوع' },
  { data: 'ownerName', title: 'مالک' },
  { data: 'model', title: 'مدل', defaultContent: '—' },
  { data: 'manufactureYear', title: 'سال', defaultContent: '—' },
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
  {
    name: 'code',
    label: 'کد',
    type: 'text',
    col: 4,
    autoCode: true,
    showOnlyOnEdit: true,
    readOnlyOnEdit: true,
    placeholder: 'خودکار',
  },
  { name: 'plateNumber', label: 'پلاک', type: 'text', required: true, col: 4 },
  {
    name: 'vehicleTypeId',
    label: 'نوع وسیله',
    type: 'select',
    required: true,
    col: 4,
    loadOptions: async () => {
      vehicleTypeOptions = (await vehicleTypesApi.options()) ?? []
      return vehicleTypeOptions
    },
  },
  {
    name: 'vehicleOwnerId',
    label: 'مالک',
    type: 'select',
    required: true,
    col: 4,
    loadOptions: () => vehicleOwnersApi.options(),
  },
  { name: 'chassisNumber', label: 'شماره بدنه', type: 'text', col: 4 },
  { name: 'model', label: 'مدل', type: 'text', col: 4 },
  { name: 'manufactureYear', label: 'سال ساخت', type: 'number', col: 4, step: 1 },
  {
    name: 'weightTon',
    label: 'وزن (تن)',
    type: 'number',
    col: 4,
    step: '0.001',
    showWhen: (form) => form._typeCode === 'BUNKER',
  },
  {
    name: 'volume',
    label: 'حجم',
    type: 'number',
    col: 4,
    step: '0.001',
    showWhen: (form) => form._typeCode === 'BUNKER',
  },
  {
    name: 'defaultIncomeSharePercent',
    label: 'سهم پیش‌فرض درآمد (٪)',
    type: 'number',
    col: 4,
    step: '0.01',
  },
  {
    name: 'defaultDriverId',
    label: 'راننده پیش‌فرض',
    type: 'select',
    col: 4,
    loadOptions: () => driversApi.options(),
    showWhen: (form) => form._typeCode !== 'BUNKER',
  },
  { name: 'isActive', label: 'فعال', type: 'switch', default: true, col: 4 },
  {
    name: '_typeCode',
    skipOnSubmit: true,
    default: '',
    showWhen: () => false,
    fromRow: (row) => resolveTypeCode(row.vehicleTypeId, row.typeCode),
  },
]

export default function VehiclesPage() {
  const handleFormChange = useCallback((name, value) => {
    if (name !== 'vehicleTypeId') return null
    const typeCode = resolveTypeCode(value)
    const patch = { _typeCode: typeCode }
    if (typeCode === 'BUNKER') {
      patch.defaultDriverId = ''
    }
    return patch
  }, [])

  return (
    <CrudTablePage
      title="وسایل نقلیه"
      createLabel="وسیله جدید"
      api={vehiclesApi}
      idField="vehicleId"
      nameField="plateNumber"
      columns={columns}
      fields={fields}
      permissionPath="/transport/vehicles"
      onFormChange={handleFormChange}
    />
  )
}
