import { useEffect, useMemo, useState } from 'react'
import DataTable from '../../lib/dataTableSetup'
import {
  createExchangeHistoryDataTableAjax,
  fetchCurrenciesList,
} from '../../services/currenciesApi'

const dataTableLanguage = {
  emptyTable: 'داده‌ای برای نمایش وجود ندارد',
  info: 'نمایش _START_ تا _END_ از _TOTAL_ ردیف',
  infoEmpty: 'رکوردی یافت نشد',
  infoFiltered: '(فیلتر شده از _MAX_ ردیف)',
  lengthMenu: 'نمایش _MENU_ ردیف',
  loadingRecords: 'در حال بارگذاری...',
  processing: 'در حال پردازش...',
  search: 'جستجو:',
  zeroRecords: 'رکوردی یافت نشد',
  paginate: {
    first: 'اول',
    last: 'آخر',
    next: 'بعدی',
    previous: 'قبلی',
  },
}

function formatRate(value) {
  if (value == null || value === '') return '—'
  return Number(value).toLocaleString('fa-IR', { maximumFractionDigits: 8 })
}

function formatDate(value) {
  if (!value) return '—'
  return new Date(value).toLocaleString('fa-IR')
}

function formatChange(current, previous) {
  if (previous == null || previous === 0) return '—'
  const pct = ((current - previous) / previous) * 100
  const sign = pct > 0 ? '+' : ''
  return `${sign}${pct.toLocaleString('fa-IR', { maximumFractionDigits: 2 })}٪`
}

function ExchangeHistoryPage() {
  const [loadError, setLoadError] = useState('')
  const [currencies, setCurrencies] = useState([])
  const [filterCurrencyId, setFilterCurrencyId] = useState('')

  useEffect(() => {
    fetchCurrenciesList()
      .then((list) => setCurrencies(list.filter((c) => !c.isBaseCurrency)))
      .catch(() => setCurrencies([]))
  }, [])

  const tableOptions = useMemo(
    () => ({
      processing: true,
      serverSide: true,
      ajax: createExchangeHistoryDataTableAjax(
        setLoadError,
        filterCurrencyId ? Number(filterCurrencyId) : null,
      ),
      paging: true,
      searching: true,
      ordering: true,
      info: true,
      scrollX: false,
      autoWidth: false,
      responsive: true,
      stripeClasses: ['odd', 'even'],
      order: [[4, 'desc']],
      pageLength: 15,
      lengthMenu: [10, 15, 25, 50, 100],
      language: dataTableLanguage,
      layout: {
        topStart: {
          search: { placeholder: 'جستجو...' },
          pageLength: { menu: [10, 15, 25, 50, 100] },
        },
        topEnd: null,

        bottomStart: 'info',
        bottomEnd: {
          paging: { firstLast: true, previousNext: true, numbers: 5 },
        },
      },
      columns: [
        { data: 'rowNumber', name: 'rowNumber' },
        { data: 'currencyName', name: 'currencyName' },
        { data: 'previousBaseUnitsPerUnit', name: 'previousBaseUnitsPerUnit' },
        { data: 'baseUnitsPerUnit', name: 'baseUnitsPerUnit' },
        { data: 'effectiveFrom', name: 'effectiveFrom' },
        { data: 'effectiveTo', name: 'effectiveTo' },
        { data: null, name: 'changePercent' },
        { data: 'changeReason', name: 'changeReason' },
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
          targets: 1,
          render: (_data, _type, row) =>
            `${row.currencyName} <span class="text-muted small">(${row.currencyCode})</span>`,
        },
        {
          targets: 2,
          className: 'text-center',
          render: (data) => formatRate(data),
        },
        {
          targets: 3,
          className: 'text-center',
          render: (data) => formatRate(data),
        },
        {
          targets: 4,
          className: 'text-center',
          render: (data) => formatDate(data),
        },
        {
          targets: 5,
          className: 'text-center',
          render: (data) => (data ? formatDate(data) : '<span class="badge badge-active">جاری</span>'),
        },
        {
          targets: 6,
          orderable: false,
          searchable: false,
          className: 'text-center',
          render: (_data, _type, row) =>
            formatChange(row.baseUnitsPerUnit, row.previousBaseUnitsPerUnit),
        },
        {
          targets: 7,
          render: (data) => data || '—',
        },
      ],
    }),
    [filterCurrencyId],
  )

  return (
    <div className="users-page">
      <div className="content-card card border-0 h-100">
        <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between gap-3 flex-wrap">
          <div>
            <h2 className="card-title mb-1">نوسانات نرخ ارز</h2>
            <p className="text-muted small mb-0">
              تاریخچه تغییرات نرخ هر ارز نسبت به ارز پایه
            </p>
          </div>
          <div className="d-flex align-items-center gap-2">
            <label className="form-label mb-0 small text-muted" htmlFor="history-currency-filter">
              فیلتر ارز
            </label>
            <select
              id="history-currency-filter"
              className="form-select form-select-sm"
              style={{ minWidth: '180px' }}
              value={filterCurrencyId}
              onChange={(e) => setFilterCurrencyId(e.target.value)}
            >
              <option value="">همه ارزها</option>
              {currencies.map((currency) => (
                <option key={currency.currencyID} value={currency.currencyID}>
                  {currency.name} ({currency.currencyCode})
                </option>
              ))}
            </select>
          </div>
        </div>

        <div className="card-body card-body-table">
          {loadError && (
            <div className="alert alert-danger py-2 users-load-error mb-0">
              {loadError}
            </div>
          )}

          <div className="users-table-wrapper">
            <DataTable
              key={filterCurrencyId || 'all'}
              className="table table-hover w-100 align-middle"
              options={tableOptions}
            >
              <thead>
                <tr>
                  <th>#</th>
                  <th>ارز</th>
                  <th>نرخ قبلی</th>
                  <th>نرخ جدید</th>
                  <th>از تاریخ</th>
                  <th>تا تاریخ</th>
                  <th>تغییر</th>
                  <th>دلیل</th>
                </tr>
              </thead>
            </DataTable>
          </div>
        </div>
      </div>
    </div>
  )
}

export default ExchangeHistoryPage
