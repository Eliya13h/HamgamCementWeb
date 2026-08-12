import CrudTablePage from '../../components/common/CrudTablePage'
import { fetchMeaurmentOptions } from '../../services/productsApi'
import { warehousesApi } from '../../services/inventoryApi'

const warehouseTypeOptions = [
  { value: 1, label: 'انبار مواد خام' },
  { value: 2, label: 'انبار مواد نیمه‌خام' },
  { value: 3, label: 'انبار مواد پردازش‌شده' },
]

const columns = [
  { data: 'name', title: 'نام انبار' },
  { data: 'warehouseTypeLabel', title: 'نوع انبار', orderable: false },
  { data: 'location', title: 'موقعیت', orderable: false },
  {
    data: 'capacityText',
    title: 'ظرفیت',
    orderable: false,
    className: 'text-end',
    render: (data) => data ?? '—',
  },
  {
    data: 'fillText',
    title: 'ظرفیت فعلی',
    orderable: false,
    className: 'text-end',
    render: (data, _type, row) => {
      if (!data || row.fillPercent == null) {
        return '<span class="text-muted">ظرفیت تعریف نشده</span>'
      }
      const percent = Math.max(0, Math.min(100, Number(row.fillPercent)))
      const tone =
        percent >= 90 ? 'is-critical' : percent >= 70 ? 'is-warning' : 'is-ok'
      return `
        <div class="warehouse-fill-cell ${tone}" title="${data}">
          <div class="warehouse-fill-bar" aria-hidden="true">
            <span style="width:${percent}%"></span>
          </div>
          <span class="warehouse-fill-text">${data}</span>
        </div>
      `
    },
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
  { name: 'name', label: 'نام انبار', type: 'text', required: true },
  {
    name: 'warehouseType',
    label: 'نوع انبار',
    type: 'select',
    required: true,
    options: warehouseTypeOptions,
    default: 1,
  },
  { name: 'location', label: 'موقعیت', type: 'text' },
  { name: 'capacity', label: 'ظرفیت', type: 'number', step: 'any' },
  {
    name: 'capacityMeaurmentId',
    label: 'واحد ظرفیت',
    type: 'select',
    loadOptions: fetchMeaurmentOptions,
  },
  { name: 'description', label: 'توضیحات', type: 'textarea' },
  { name: 'isActive', label: 'فعال', type: 'switch', default: true },
]

function WarehousesPage() {
  return (
    <CrudTablePage
      title="انبارها"
      createLabel="انبار جدید"
      api={warehousesApi}
      idField="warehouseId"
      nameField="name"
      columns={columns}
      fields={fields}
      permissionPath="/inventory/warehouses"
    />
  )
}

export default WarehousesPage
