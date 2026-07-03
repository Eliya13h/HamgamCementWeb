import { useState } from 'react'
import CrudTablePage, { formatAmount, formatDate } from './CrudTablePage'
import {
  fetchVehicleOptions,
  maintenancesApi,
  partsApi,
} from '../../services/transportApi'

const vehicleField = {
  name: 'vehicleId',
  label: 'وسیله نقلیه',
  type: 'select',
  required: true,
  col: 6,
  loadOptions: fetchVehicleOptions,
}

const TABS = [
  {
    id: 'maintenances',
    label: 'تعمیرات و نگهداری',
    title: 'تعمیرات و نگهداری',
    createLabel: 'ثبت تعمیر/سرویس',
    defaultOrder: [[3, 'desc']],
    api: maintenancesApi,
    idField: 'vehicleMaintenanceId',
    nameField: 'title',
    columns: [
      { data: 'vehicleLabel', title: 'وسیله نقلیه', orderable: false },
      { data: 'title', title: 'عنوان' },
      {
        data: 'maintenanceDate',
        title: 'تاریخ',
        className: 'text-center',
        render: (data) => formatDate(data),
      },
      {
        data: 'odometerKm',
        title: 'کیلومتر',
        className: 'text-center',
        render: (data) => formatAmount(data),
      },
      {
        data: 'cost',
        title: 'هزینه',
        className: 'text-center',
        render: (data) => formatAmount(data),
      },
      {
        data: 'nextServiceDate',
        title: 'سرویس بعدی',
        className: 'text-center',
        render: (data) => formatDate(data),
      },
    ],
    fields: [
      vehicleField,
      { name: 'title', label: 'عنوان تعمیر/سرویس', type: 'text', required: true, col: 6 },
      { name: 'maintenanceDate', label: 'تاریخ تعمیر', type: 'date', required: true, col: 4 },
      { name: 'cost', label: 'هزینه', type: 'number', required: true, col: 4 },
      { name: 'odometerKm', label: 'کیلومتر شمار', type: 'number', col: 4 },
      { name: 'workshopName', label: 'تعمیرگاه / تعمیرکار', type: 'text', col: 6 },
      { name: 'nextServiceDate', label: 'تاریخ سرویس بعدی', type: 'date', col: 6 },
      { name: 'description', label: 'توضیحات', type: 'textarea' },
    ],
  },
  {
    id: 'parts',
    label: 'تعویض قطعات',
    title: 'تعویض قطعات و لوازم مصرفی',
    createLabel: 'ثبت تعویض قطعه',
    defaultOrder: [[6, 'desc']],
    api: partsApi,
    idField: 'vehiclePartReplacementId',
    nameField: 'partName',
    columns: [
      { data: 'vehicleLabel', title: 'وسیله نقلیه', orderable: false },
      { data: 'partName', title: 'نام قطعه' },
      {
        data: 'quantity',
        title: 'تعداد',
        className: 'text-center',
        render: (data) => formatAmount(data),
      },
      {
        data: 'unitCost',
        title: 'قیمت واحد',
        className: 'text-center',
        render: (data) => formatAmount(data),
      },
      {
        data: 'totalCost',
        title: 'هزینه کل',
        className: 'text-center',
        render: (data) => formatAmount(data),
      },
      {
        data: 'replacementDate',
        title: 'تاریخ تعویض',
        className: 'text-center',
        render: (data) => formatDate(data),
      },
    ],
    fields: [
      vehicleField,
      { name: 'partName', label: 'نام قطعه', type: 'text', required: true, col: 6 },
      { name: 'quantity', label: 'تعداد', type: 'number', required: true, default: 1, col: 4 },
      { name: 'unitCost', label: 'قیمت واحد', type: 'number', required: true, col: 4 },
      { name: 'replacementDate', label: 'تاریخ تعویض', type: 'date', required: true, col: 4 },
      { name: 'odometerKm', label: 'کیلومتر شمار', type: 'number', col: 4 },
      { name: 'description', label: 'توضیحات', type: 'textarea' },
    ],
  },
]

function MaintenancePage() {
  const [activeTab, setActiveTab] = useState(TABS[0].id)
  const tab = TABS.find((t) => t.id === activeTab)

  return (
    <div className="content-card card border-0 h-100">
      <div className="card-header bg-transparent border-0 pt-3 px-4 pb-0">
        <ul className="nav nav-tabs card-header-tabs">
          {TABS.map((t) => (
            <li className="nav-item" key={t.id}>
              <button
                type="button"
                className={`nav-link ${t.id === activeTab ? 'active' : ''}`}
                onClick={() => setActiveTab(t.id)}
              >
                {t.label}
              </button>
            </li>
          ))}
        </ul>
      </div>

      <CrudTablePage
        key={tab.id}
        embedded
        title={tab.title}
        createLabel={tab.createLabel}
        api={tab.api}
        idField={tab.idField}
        nameField={tab.nameField}
        columns={tab.columns}
        fields={tab.fields}
        defaultOrder={tab.defaultOrder}
        permissionPath="/transport/maintenance"
      />
    </div>
  )
}

export default MaintenancePage
