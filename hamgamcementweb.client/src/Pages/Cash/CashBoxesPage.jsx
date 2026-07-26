import CrudTablePage from '../Transport/CrudTablePage'
import {
  cashBoxesApi,
  fetchCashBoxOptions,
  fetchCashBoxUserOptions,
} from '../../services/ledgerApi'

const columns = [
  { data: 'code', title: 'کد' },
  { data: 'name', title: 'نام' },
  { data: 'parentName', title: 'صندوق بالاتر', orderable: false },
  {
    data: 'balancesText',
    title: 'موجودی ارزها',
    orderable: false,
    defaultContent: '',
  },
  {
    data: 'userCount',
    title: 'کاربران',
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
  {
    name: 'code',
    label: 'کد (خودکار)',
    type: 'text',
    col: 4,
    showOnlyOnEdit: true,
    readOnlyOnEdit: true,
  },
  { name: 'name', label: 'نام', type: 'text', required: true, col: 4 },
  {
    name: 'parentCashBoxId',
    label: 'صندوق بالاتر',
    type: 'select',
    col: 4,
    loadOptions: async () => {
      const rows = await fetchCashBoxOptions()
      return rows.map((r) => ({
        value: String(r.value),
        label: r.label,
      }))
    },
  },
  {
    name: 'userIds',
    label: 'کاربران صندوق',
    type: 'multiselect',
    col: 8,
    fromRow: (row) =>
      String(row.userIdsText ?? '')
        .split(/[,\s]+/)
        .map((x) => x.trim())
        .filter(Boolean),
    loadOptions: async () => {
      const rows = await fetchCashBoxUserOptions()
      return rows.map((r) => ({
        value: String(r.value),
        label: r.label,
      }))
    },
  },
  { name: 'description', label: 'توضیحات', type: 'textarea', col: 12 },
  { name: 'isActive', label: 'فعال', type: 'switch', col: 4, default: true },
]

const cashBoxesPageApi = {
  createDataTableAjax: cashBoxesApi.createDataTableAjax,
  create: (payload) =>
    cashBoxesApi.create({
      name: payload.name,
      parentCashBoxId: payload.parentCashBoxId,
      userIds: payload.userIds ?? [],
      description: payload.description,
      isActive: payload.isActive,
    }),
  update: (id, payload) =>
    cashBoxesApi.update(id, {
      name: payload.name,
      parentCashBoxId: payload.parentCashBoxId,
      userIds: payload.userIds ?? [],
      description: payload.description,
      isActive: payload.isActive,
    }),
  remove: cashBoxesApi.remove,
}

function CashBoxesPage() {
  return (
    <CrudTablePage
      title="صندوق‌ها"
      createLabel="صندوق جدید"
      api={cashBoxesPageApi}
      idField="cashBoxId"
      nameField="name"
      columns={columns}
      fields={fields}
      permissionPath="/accounting/expenses"
      canDeleteRow={() => false}
    />
  )
}

export default CashBoxesPage
