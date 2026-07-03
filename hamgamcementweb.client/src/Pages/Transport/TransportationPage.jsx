import { useCallback } from 'react'
import CrudTablePage, { formatAmount, formatJalaliDate } from './CrudTablePage'
import {
  fetchDriverOptions,
  fetchRouteOptions,
  fetchVehicleOptions,
  tripsApi,
} from '../../services/transportApi'

const STATUS_BADGES = {
  1: '<span class="badge text-bg-secondary">برنامه‌ریزی شده</span>',
  2: '<span class="badge text-bg-info">در مسیر</span>',
  3: '<span class="badge badge-active">تکمیل شده</span>',
  4: '<span class="badge badge-inactive">لغو شده</span>',
}

const STATUS_OPTIONS = [
  { value: 1, label: 'برنامه‌ریزی شده' },
  { value: 2, label: 'در مسیر' },
  { value: 3, label: 'تکمیل شده' },
  { value: 4, label: 'لغو شده' },
]

const columns = [
  { data: 'tripNumber', title: 'کد سفر' },
  { data: 'vehicleLabel', title: 'وسیله نقلیه', orderable: false },
  { data: 'routeName', title: 'مسیر', orderable: false },
  { data: 'driverName', title: 'راننده', orderable: false, render: (data) => data ?? '—' },
  {
    data: 'departureDate',
    title: 'تاریخ حرکت',
    className: 'text-center',
    render: (data) => formatJalaliDate(data),
  },
  {
    data: 'arrivalDate',
    title: 'تاریخ رسیدن',
    className: 'text-center',
    render: (data) => formatJalaliDate(data),
  },
  {
    data: 'tripRevenue',
    title: 'درآمد سفر',
    className: 'text-center',
    render: (data) => formatAmount(data),
  },
  {
    data: 'cargoWeightTon',
    title: 'وزن بار (تن)',
    className: 'text-center',
    render: (data) => formatAmount(data),
  },
  {
    data: 'status',
    title: 'وضعیت',
    className: 'text-center',
    render: (data) => STATUS_BADGES[data] ?? '—',
  },
]

const vehicleOptionsRef = { current: [] }

async function loadVehicleOptions() {
  const options = await fetchVehicleOptions()
  vehicleOptionsRef.current = options ?? []
  return vehicleOptionsRef.current
}

const fields = [
  {
    name: 'tripNumber',
    label: 'کد سفر',
    type: 'readonly',
    showOnlyOnEdit: true,
    autoCode: true,
    col: 4,
  },
  {
    name: 'vehicleId',
    label: 'وسیله نقلیه',
    type: 'select',
    required: true,
    col: 4,
    loadOptions: loadVehicleOptions,
  },
  {
    name: 'transportRouteId',
    label: 'مسیر',
    type: 'select',
    required: true,
    col: 4,
    loadOptions: fetchRouteOptions,
  },
  {
    name: 'driverId',
    label: 'راننده سفر',
    type: 'select',
    col: 4,
    loadOptions: fetchDriverOptions,
    placeholder: 'پیش‌فرض: راننده وسیله',
    fromRow: (row) => row.driverId ?? row.effectiveDriverId ?? '',
  },
  { name: 'cargoDescription', label: 'شرح بار', type: 'text', col: 8 },
  { name: 'cargoWeightTon', label: 'وزن بار (تن)', type: 'number', col: 4 },
  {
    name: 'departureDate',
    label: 'تاریخ حرکت (شمسی)',
    type: 'jalali-date',
    required: true,
    col: 4,
  },
  {
    name: 'arrivalDate',
    label: 'تاریخ رسیدن (شمسی)',
    type: 'jalali-date',
    col: 4,
  },
  {
    name: 'tripRevenue',
    label: 'درآمد سفر',
    type: 'number',
    required: true,
    default: 0,
    col: 4,
  },
  {
    name: 'status',
    label: 'وضعیت سفر',
    type: 'select',
    required: true,
    col: 4,
    options: STATUS_OPTIONS,
    default: 1,
  },
  { name: 'fuelConsumedLiters', label: 'سوخت مصرفی (لیتر)', type: 'number', col: 4 },
  { name: 'odometerStart', label: 'کیلومتر شروع', type: 'number', col: 4 },
  { name: 'odometerEnd', label: 'کیلومتر پایان', type: 'number', col: 4 },
  { name: 'description', label: 'توضیحات', type: 'textarea' },
]

function TransportationPage() {
  const handleFormChange = useCallback((name, value) => {
    if (name !== 'vehicleId' || !value) {
      return null
    }

    const vehicle = vehicleOptionsRef.current.find(
      (item) => String(item.value) === String(value),
    )
    if (vehicle?.defaultDriverId) {
      return { driverId: vehicle.defaultDriverId }
    }

    return { driverId: '' }
  }, [])

  return (
    <CrudTablePage
      title="حمل و نقل (سفرها)"
      createLabel="سفر جدید"
      api={tripsApi}
      idField="transportTripId"
      nameField="tripNumber"
      columns={columns}
      fields={fields}
      defaultOrder={[[5, 'desc']]}
      permissionPath="/transport/shipping"
      onFormChange={handleFormChange}
    />
  )
}

export default TransportationPage
