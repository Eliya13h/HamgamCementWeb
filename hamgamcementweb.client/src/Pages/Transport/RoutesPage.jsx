import CrudTablePage, { formatAmount } from './CrudTablePage'
import { routesApi } from '../../services/transportApi'

const columns = [
  { data: 'code', title: 'کد مسیر' },
  { data: 'name', title: 'نام مسیر' },
  { data: 'origin', title: 'مبدأ' },
  { data: 'destination', title: 'مقصد' },
  {
    data: 'distanceKm',
    title: 'مسافت (km)',
    className: 'text-center',
    render: (data) => formatAmount(data),
  },
  {
    data: 'estimatedDays',
    title: 'مدت تقریبی (روز)',
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
    label: 'کد مسیر',
    type: 'readonly',
    showOnlyOnEdit: true,
    autoCode: true,
    col: 4,
  },
  { name: 'name', label: 'نام مسیر', type: 'text', required: true, col: 8 },
  { name: 'origin', label: 'مبدأ', type: 'text', required: true, col: 6 },
  { name: 'originCountry', label: 'کشور مبدأ', type: 'text', col: 6 },
  { name: 'destination', label: 'مقصد', type: 'text', required: true, col: 6 },
  { name: 'distanceKm', label: 'مسافت (کیلومتر)', type: 'number', col: 3 },
  { name: 'estimatedDays', label: 'مدت تقریبی (روز)', type: 'number', col: 3 },
  { name: 'description', label: 'توضیحات', type: 'textarea' },
  { name: 'isActive', label: 'فعال', type: 'switch', default: true },
]

function RoutesPage() {
  return (
    <CrudTablePage
      title="مسیرهای حمل و نقل"
      createLabel="مسیر جدید"
      api={routesApi}
      idField="transportRouteId"
      nameField="name"
      columns={columns}
      fields={fields}
      permissionPath="/transport/routes"
    />
  )
}

export default RoutesPage
