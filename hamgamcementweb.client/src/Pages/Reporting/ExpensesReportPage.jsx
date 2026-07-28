import { useMemo, useRef, useState } from 'react'
import JalaliDateField from '../../components/common/JalaliDateField'
import DataTable from '../../lib/dataTableSetup'
import { currentJalaliYearMonth, getJalaliYearRange, toLatinIsoDate, todayGregorianIso } from '../../lib/afghanSolarCalendar'
import { expensesApi } from '../../services/financeApi'
import { getJournalReportUrl } from '../../services/journalApi'
import { dataTableLanguage, formatAmount, formatJalaliDate } from '../Transport/CrudTablePage'

function ExpensesReportPage() {
  const tableRef = useRef(null)
  const yearStart = useMemo(() => {
    const { year } = currentJalaliYearMonth()
    return getJalaliYearRange(year).from
  }, [])

  const [dateFrom, setDateFrom] = useState(yearStart)
  const [dateTo, setDateTo] = useState(todayGregorianIso())
  const [loadError, setLoadError] = useState('')
  const [filterError, setFilterError] = useState('')

  const openJournalReport = () => {
    setFilterError('')
    if (!dateFrom || !dateTo) {
      setFilterError('لطفاً بازه تاریخ را انتخاب کنید.')
      return
    }
    if (dateFrom > dateTo) {
      setFilterError('تاریخ شروع نباید بعد از تاریخ پایان باشد.')
      return
    }
    window.open(
      getJournalReportUrl('expense', toLatinIsoDate(dateFrom) || dateFrom, toLatinIsoDate(dateTo) || dateTo),
      '_blank',
      'noopener,noreferrer',
    )
  }

  const tableOptions = useMemo(
    () => ({
      processing: true,
      serverSide: true,
      ajax: expensesApi.createDataTableAjax(setLoadError),
      paging: true,
      searching: true,
      ordering: true,
      info: true,
      scrollX: true,
      autoWidth: false,
      responsive: true,
      stripeClasses: ['odd', 'even'],
      order: [[2, 'desc']],
      pageLength: 15,
      language: dataTableLanguage,
      columns: [
        { data: 'rowNumber', name: 'rowNumber' },
        { data: 'title', name: 'title', title: 'عنوان' },
        {
          data: 'expenseDate',
          name: 'expenseDate',
          title: 'تاریخ',
          render: (data) => formatJalaliDate(data),
        },
        { data: 'categoryName', name: 'categoryName', title: 'دسته‌بندی', defaultContent: '—' },
        { data: 'sourceLabel', name: 'sourceLabel', title: 'منبع', defaultContent: '—', orderable: false },
        { data: 'supplierName', name: 'supplierName', title: 'تأمین‌کننده', defaultContent: '—', orderable: false },
        {
          data: 'amount',
          name: 'amount',
          title: 'مبلغ',
          className: 'text-end',
          render: (data) => formatAmount(data),
        },
        {
          data: 'amountInBaseCurrency',
          name: 'amountInBaseCurrency',
          title: 'مبلغ (ارز پایه)',
          className: 'text-end',
          render: (data) => formatAmount(data),
        },
      ],
      columnDefs: [
        { targets: 0, orderable: false, searchable: false, width: '56px', className: 'text-center' },
      ],
    }),
    [],
  )

  return (
    <div className="content-card card border-0">
      <div className="card-body p-4">
        <h2 className="card-title mb-2">گزارش مصارف</h2>
        <p className="text-muted mb-4">
          فهرست مصارف ثبت‌شده و امکان ساخت گزارش روزنامچه مصارف در Stimulsoft.
        </p>

        {(filterError || loadError) && (
          <div className="alert alert-danger py-2 mb-3">{filterError || loadError}</div>
        )}

        <div className="row g-3 align-items-end mb-4">
          <div className="col-md-3">
            <label className="form-label">از تاریخ</label>
            <JalaliDateField value={dateFrom} onChange={setDateFrom} />
          </div>
          <div className="col-md-3">
            <label className="form-label">تا تاریخ</label>
            <JalaliDateField value={dateTo} onChange={setDateTo} />
          </div>
          <div className="col-md-3">
            <button type="button" className="btn btn-primary w-100" onClick={openJournalReport}>
              گزارش روزنامچه مصارف
            </button>
          </div>
        </div>

        <h3 className="h6 mb-3">فهرست مصارف</h3>
        <div className="users-table-wrapper">
          <DataTable
            ref={tableRef}
            className="table table-hover w-100 align-middle"
            options={tableOptions}
          >
            <thead>
              <tr>
                <th>#</th>
                <th>عنوان</th>
                <th>تاریخ</th>
                <th>دسته‌بندی</th>
                <th>منبع</th>
                <th>تأمین‌کننده</th>
                <th>مبلغ</th>
                <th>مبلغ (ارز پایه)</th>
              </tr>
            </thead>
          </DataTable>
        </div>
      </div>
    </div>
  )
}

export default ExpensesReportPage
