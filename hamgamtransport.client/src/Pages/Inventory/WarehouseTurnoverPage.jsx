import { useEffect, useMemo, useState } from 'react'
import JalaliDateField from '../../components/common/JalaliDateField'
import DataTable from '../../lib/dataTableSetup'
import { fetchProductOptions } from '../../services/productsApi'
import {
  createWarehouseTurnoverDataTableAjax,
  fetchWarehouseOptions,
} from '../../services/inventoryApi'
import { dataTableLanguage, formatAmount, formatJalaliDate } from '../../components/common/CrudTablePage'

const movementTypeBadge = {
  PurchaseIn: '<span class="badge badge-active">ورود خرید</span>',
  PurchaseReturnOut: '<span class="badge badge-warning">برگشت خرید</span>',
  SaleOut: '<span class="badge bg-primary">خروج فروش</span>',
  SaleReturnIn: '<span class="badge bg-info text-dark">برگشت فروش</span>',
  StocktakingAdjust: '<span class="badge bg-secondary">تعدیل انبارگردانی</span>',
  ProductionIn: '<span class="badge badge-active">ورود از تولید</span>',
  ProductionOut: '<span class="badge badge-warning">مصرف تولید</span>',
  TransferIn: '<span class="badge bg-info text-dark">ورود انتقال</span>',
  TransferOut: '<span class="badge bg-primary">خروج انتقال</span>',
}

function WarehouseTurnoverPage() {
  const [loadError, setLoadError] = useState('')
  const [warehouseOptions, setWarehouseOptions] = useState([])
  const [productOptions, setProductOptions] = useState([])
  const [warehouseId, setWarehouseId] = useState('')
  const [productId, setProductId] = useState('')
  const [dateFrom, setDateFrom] = useState('')
  const [dateTo, setDateTo] = useState('')

  useEffect(() => {
    fetchWarehouseOptions()
      .then((items) => setWarehouseOptions(items ?? []))
      .catch(() => setWarehouseOptions([]))
    fetchProductOptions()
      .then((items) => setProductOptions(items ?? []))
      .catch(() => setProductOptions([]))
  }, [])

  const filters = useMemo(
    () => ({
      warehouseId: warehouseId ? Number(warehouseId) : null,
      productId: productId ? Number(productId) : null,
      dateFrom: dateFrom || null,
      dateTo: dateTo || null,
    }),
    [warehouseId, productId, dateFrom, dateTo],
  )

  const showRunningBalance = Boolean(productId)

  const tableOptions = useMemo(
    () => ({
      processing: true,
      serverSide: true,
      ajax: warehouseId
        ? createWarehouseTurnoverDataTableAjax(setLoadError, filters)
        : (_data, callback) => {
            callback({
              draw: _data.draw,
              recordsTotal: 0,
              recordsFiltered: 0,
              data: [],
            })
          },
      paging: true,
      searching: true,
      ordering: true,
      info: true,
      scrollX: false,
      autoWidth: false,
      responsive: true,
      stripeClasses: ['odd', 'even'],
      order: [[2, 'asc']],
      pageLength: 25,
      lengthMenu: [10, 15, 25, 50, 100],
      language: dataTableLanguage,
      layout: {
        topStart: {
          search: { placeholder: 'جستجو...' },
          pageLength: { menu: [10, 15, 25, 50, 100] },
        },
        topEnd: null,
        bottomStart: 'info',
        bottomEnd: { paging: { firstLast: true, previousNext: true, numbers: 5 } },
      },
      columns: [
        { data: 'rowNumber', name: 'rowNumber' },
        { data: 'warehouseName', name: 'warehouseName', visible: false },
        { data: 'movementDate', name: 'movementDate' },
        { data: 'movementTypeCode', name: 'movementTypeCode' },
        { data: 'documentNumber', name: 'documentNumber' },
        { data: 'counterpartyName', name: 'counterpartyName' },
        { data: 'productCode', name: 'productCode' },
        { data: 'productName', name: 'productName' },
        { data: 'quantityIn', name: 'quantityIn' },
        { data: 'quantityOut', name: 'quantityOut' },
        ...(showRunningBalance
          ? [{ data: 'runningBalanceInBase', name: 'runningBalanceInBase' }]
          : []),
        { data: 'quantity', name: 'quantity' },
        { data: 'unitPrice', name: 'unitPrice' },
        { data: 'lineTotal', name: 'lineTotal' },
      ],
      columnDefs: [
        {
          targets: 0,
          orderable: false,
          searchable: false,
          width: '56px',
          className: 'text-center',
        },
        {
          targets: 2,
          className: 'text-center',
          render: (data) => formatJalaliDate(data),
        },
        {
          targets: 3,
          className: 'text-center',
          render: (_data, _type, row) =>
            movementTypeBadge[row.movementTypeCode] ?? row.movementType,
        },
        {
          targets: 4,
          className: 'text-center',
        },
        {
          targets: 5,
          render: (data) => data || '—',
        },
        {
          targets: [8, 9],
          className: 'text-end',
          render: (data) => (Number(data) > 0 ? formatAmount(data) : '—'),
        },
        ...(showRunningBalance
          ? [
              {
                targets: 10,
                className: 'text-end',
                render: (data) => (data != null ? formatAmount(data) : '—'),
              },
              {
                targets: 11,
                className: 'text-end',
                render: (data, type, row) =>
                  type === 'display'
                    ? `${formatAmount(data)} ${row.meaurmentSymbol || row.meaurmentName || ''}`.trim()
                    : data,
              },
              {
                targets: 12,
                className: 'text-end',
                render: (data) => (Number(data) > 0 ? formatAmount(data) : '—'),
              },
              {
                targets: 13,
                className: 'text-end',
                render: (data) => (Number(data) > 0 ? formatAmount(data) : '—'),
              },
            ]
          : [
              {
                targets: 10,
                className: 'text-end',
                render: (data, type, row) =>
                  type === 'display'
                    ? `${formatAmount(data)} ${row.meaurmentSymbol || row.meaurmentName || ''}`.trim()
                    : data,
              },
              {
                targets: 11,
                className: 'text-end',
                render: (data) => (Number(data) > 0 ? formatAmount(data) : '—'),
              },
              {
                targets: 12,
                className: 'text-end',
                render: (data) => (Number(data) > 0 ? formatAmount(data) : '—'),
              },
            ]),
      ],
    }),
    [warehouseId, filters, showRunningBalance],
  )

  const tableKey = `${warehouseId}-${productId}-${dateFrom}-${dateTo}-${showRunningBalance}`

  return (
    <div className="users-page">
      <div className="content-card card border-0 h-100">
        <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0">
          <h2 className="card-title mb-1">گردش کالا (دفتر انبار)</h2>
          <p className="text-muted small mb-0">
            گزارش ورود و خروج اقلام هر انبار بر اساس خرید، فروش، تولید، انتقال و انبارگردانی
          </p>
        </div>

        <div className="card-body">
          <div className="row g-3 mb-3 align-items-end">
            <div className="col-md-3">
              <label className="form-label" htmlFor="turnover-warehouse">
                انبار <span className="text-danger">*</span>
              </label>
              <select
                id="turnover-warehouse"
                className="form-select"
                value={warehouseId}
                onChange={(e) => setWarehouseId(e.target.value)}
              >
                <option value="">انتخاب انبار...</option>
                {warehouseOptions.map((o) => (
                  <option key={o.value} value={o.value}>
                    {o.label}
                  </option>
                ))}
              </select>
            </div>
            <div className="col-md-3">
              <label className="form-label" htmlFor="turnover-product">
                محصول
              </label>
              <select
                id="turnover-product"
                className="form-select"
                value={productId}
                onChange={(e) => setProductId(e.target.value)}
              >
                <option value="">همه محصولات</option>
                {productOptions.map((o) => (
                  <option key={o.value} value={o.value}>
                    {o.label}
                  </option>
                ))}
              </select>
            </div>
            <div className="col-md-2">
              <label className="form-label">از تاریخ</label>
              <JalaliDateField value={dateFrom} onChange={setDateFrom} />
            </div>
            <div className="col-md-2">
              <label className="form-label">تا تاریخ</label>
              <JalaliDateField value={dateTo} onChange={setDateTo} />
            </div>
          </div>

          {!warehouseId && (
            <div className="alert alert-secondary py-2 mb-3">
              برای مشاهده گردش، ابتدا انبار را انتخاب کنید.
            </div>
          )}

          {productId && (
            <div className="alert alert-info py-2 mb-3">
              با انتخاب محصول، ستون «مانده (پایه)» به‌صورت تجمعی نمایش داده می‌شود.
            </div>
          )}

          {loadError && (
            <div className="alert alert-danger py-2 users-load-error mb-3">{loadError}</div>
          )}

          <div className="users-table-wrapper">
            <DataTable
              key={tableKey}
              className="table table-hover w-100 align-middle"
              options={tableOptions}
            >
              <thead>
                <tr>
                  <th>#</th>
                  <th>انبار</th>
                  <th>تاریخ</th>
                  <th>نوع</th>
                  <th>شماره سند</th>
                  <th>طرف حساب</th>
                  <th>کد کالا</th>
                  <th>نام کالا</th>
                  <th>ورود (پایه)</th>
                  <th>خروج (پایه)</th>
                  {showRunningBalance && <th>مانده (پایه)</th>}
                  <th>مقدار</th>
                  <th>قیمت واحد</th>
                  <th>جمع</th>
                </tr>
              </thead>
            </DataTable>
          </div>
        </div>
      </div>
    </div>
  )
}

export default WarehouseTurnoverPage
