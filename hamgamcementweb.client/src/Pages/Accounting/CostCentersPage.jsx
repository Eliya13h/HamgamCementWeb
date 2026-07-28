import CrudTablePage from '../Transport/CrudTablePage'
import { costCentersApi } from '../../services/ledgerApi'

const columns = [
  { data: 'code', title: 'کد' },
  { data: 'name', title: 'نام' },
  { data: 'description', title: 'توضیحات', orderable: false, defaultContent: '' },
  {
    data: 'isActive',
    title: 'وضعیت',
    className: 'text-center',
    render: (value) =>
      value
        ? '<span class="badge badge-active">فعال</span>'
        : '<span class="badge badge-inactive">غیرفعال</span>',
  },
]

const fields = [
  { name: 'code', label: 'کد', type: 'text', required: true, col: 4 },
  { name: 'name', label: 'نام', type: 'text', required: true, col: 8 },
  { name: 'description', label: 'توضیحات', type: 'textarea', col: 12 },
  { name: 'isActive', label: 'فعال', type: 'switch', default: true, col: 4 },
]

export default function CostCentersPage() {
  return (
    <CrudTablePage
      title="مراکز هزینه"
      createLabel="مرکز هزینه جدید"
      api={costCentersApi}
      idField="costCenterId"
      nameField="name"
      columns={columns}
      fields={fields}
      permissionPath="/accounting/cost-centers"
    />
  )
}
