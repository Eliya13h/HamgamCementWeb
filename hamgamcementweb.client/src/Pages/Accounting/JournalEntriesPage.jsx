import { useMemo, useRef, useState } from 'react'
import DataTable from '../../lib/dataTableSetup'
import {
  createServerSideTableOptions,
  formatAmount,
} from '../../lib/dataTableOptions'
import { formatJalaliDate } from '../../lib/afghanSolarCalendar'
import { journalEntriesApi } from '../../services/ledgerApi'

/** صفر سمت خالی خط سند را خالی نشان بده */
function formatLineAmount(value) {
  const num = Number(value)
  if (!Number.isFinite(num) || num === 0) return '—'
  return formatAmount(num)
}

function JournalEntriesPage() {
  const tableRef = useRef(null)
  const [loadError, setLoadError] = useState('')
  const [selected, setSelected] = useState(null)
  const [detailError, setDetailError] = useState('')
  const [detailLoading, setDetailLoading] = useState(false)

  const closeDetail = () => {
    setSelected(null)
    setDetailError('')
    setDetailLoading(false)
  }

  const openDetail = async (journalEntryId) => {
    setDetailLoading(true)
    setDetailError('')
    setSelected(null)
    try {
      const detail = await journalEntriesApi.get(journalEntryId)
      setSelected(detail)
      setLoadError('')
    } catch (err) {
      setDetailError(err.message || 'خطا در دریافت جزئیات سند')
    } finally {
      setDetailLoading(false)
    }
  }

  const tableOptions = useMemo(
    () =>
      createServerSideTableOptions({
        ajax: journalEntriesApi.createDataTableAjax(setLoadError),
        searching: true,
        order: [[2, 'desc']],
        columns: [
          { data: 'rowNumber', name: 'rowNumber', orderable: false },
          { data: 'entryNumber', name: 'entryNumber' },
          {
            data: 'entryDate',
            name: 'entryDate',
            render: (data, type) =>
              type === 'display' ? formatJalaliDate(data) : data,
          },
          { data: 'description', name: 'description', orderable: false },
          { data: 'sourceLabel', name: 'sourceLabel', orderable: false },
          {
            data: 'totalDebitInBaseCurrency',
            name: 'totalDebitInBaseCurrency',
            className: 'text-end',
            render: (data, type) =>
              type === 'display' ? formatAmount(data) : data,
          },
          {
            data: 'totalCreditInBaseCurrency',
            name: 'totalCreditInBaseCurrency',
            className: 'text-end',
            render: (data, type) =>
              type === 'display' ? formatAmount(data) : data,
          },
          {
            data: null,
            name: 'actions',
            orderable: false,
            searchable: false,
            defaultContent: '',
          },
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
            targets: -1,
            orderable: false,
            searchable: false,
            className: 'text-center',
          },
        ],
      }),
    [],
  )

  const actionSlots = useMemo(
    () => ({
      7: (_data, _type, row) => (
        <button
          type="button"
          className="btn btn-sm btn-outline-primary"
          onClick={() => openDetail(row.journalEntryId)}
        >
          مشاهده
        </button>
      ),
    }),
    [],
  )

  const showModal = detailLoading || selected || detailError

  return (
    <div className="users-page">
      <div className="content-card card border-0">
        <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0">
          <h2 className="card-title mb-0">اسناد دفترروزنامه</h2>
        </div>
        <div className="card-body card-body-table">
          {loadError && (
            <div className="alert alert-danger py-2 mb-0">{loadError}</div>
          )}
          <div className="users-table-wrapper">
            <DataTable
              ref={tableRef}
              className="table table-hover w-100 align-middle"
              options={tableOptions}
              slots={actionSlots}
            >
              <thead>
                <tr>
                  <th>#</th>
                  <th>شماره</th>
                  <th>تاریخ</th>
                  <th>شرح</th>
                  <th>منبع</th>
                  <th>بدهکار</th>
                  <th>بستانکار</th>
                  <th>عملیات</th>
                </tr>
              </thead>
            </DataTable>
          </div>
        </div>
      </div>

      {showModal && (
        <>
          <div
            className="modal-backdrop show users-modal-backdrop"
            onClick={closeDetail}
          />
          <div
            className="modal show d-block users-modal"
            tabIndex="-1"
            role="dialog"
            aria-modal="true"
            aria-labelledby="journal-entry-detail-title"
          >
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable modal-xl">
              <div className="modal-content">
                <div className="modal-header border-0 pb-0">
                  <div>
                    <h5
                      className="modal-title mb-1"
                      id="journal-entry-detail-title"
                    >
                      {selected
                        ? `سند شماره ${selected.entryNumber}`
                        : 'جزئیات سند'}
                    </h5>
                    {selected?.sourceLabel && (
                      <span className="badge rounded-pill text-bg-light border">
                        {selected.sourceLabel}
                      </span>
                    )}
                  </div>
                  <button
                    type="button"
                    className="btn-close"
                    aria-label="بستن"
                    onClick={closeDetail}
                  />
                </div>

                <div className="modal-body pt-3">
                  {detailError && (
                    <div className="alert alert-danger py-2">{detailError}</div>
                  )}

                  {detailLoading && (
                    <div className="text-center text-muted py-5">
                      در حال بارگذاری جزئیات سند...
                    </div>
                  )}

                  {selected && !detailLoading && (
                    <>
                      <div className="row g-3 mb-4">
                        <div className="col-md-3 col-6">
                          <div className="border rounded-3 p-3 h-100 bg-light bg-opacity-50">
                            <div className="text-muted small mb-1">تاریخ</div>
                            <div className="fw-semibold">
                              {formatJalaliDate(selected.entryDate)}
                            </div>
                          </div>
                        </div>
                        <div className="col-md-3 col-6">
                          <div className="border rounded-3 p-3 h-100 bg-light bg-opacity-50">
                            <div className="text-muted small mb-1">منبع</div>
                            <div className="fw-semibold">
                              {selected.sourceLabel || '—'}
                            </div>
                          </div>
                        </div>
                        <div className="col-md-3 col-6">
                          <div className="border rounded-3 p-3 h-100 bg-light bg-opacity-50">
                            <div className="text-muted small mb-1">
                              جمع بدهکار
                            </div>
                            <div className="fw-semibold text-end font-monospace">
                              {formatAmount(selected.totalDebitInBaseCurrency)}
                            </div>
                          </div>
                        </div>
                        <div className="col-md-3 col-6">
                          <div className="border rounded-3 p-3 h-100 bg-light bg-opacity-50">
                            <div className="text-muted small mb-1">
                              جمع بستانکار
                            </div>
                            <div className="fw-semibold text-end font-monospace">
                              {formatAmount(selected.totalCreditInBaseCurrency)}
                            </div>
                          </div>
                        </div>
                        <div className="col-12">
                          <div className="border rounded-3 p-3 bg-light bg-opacity-50">
                            <div className="text-muted small mb-1">شرح سند</div>
                            <div>{selected.description || '—'}</div>
                          </div>
                        </div>
                      </div>

                      <div className="d-flex align-items-center justify-content-between mb-2">
                        <h6 className="mb-0">خطوط سند</h6>
                        <span className="text-muted small">
                          {(selected.lines ?? []).length} ردیف
                        </span>
                      </div>

                      <div className="table-responsive border rounded-3">
                        <table className="table table-sm table-hover align-middle mb-0">
                          <thead className="table-light">
                            <tr>
                              <th style={{ width: 48 }}>#</th>
                              <th style={{ width: 110 }}>کد حساب</th>
                              <th>حساب</th>
                              <th>شرح</th>
                              <th className="text-end" style={{ width: 140 }}>
                                بدهکار
                              </th>
                              <th className="text-end" style={{ width: 140 }}>
                                بستانکار
                              </th>
                            </tr>
                          </thead>
                          <tbody>
                            {(selected.lines ?? []).map((line) => (
                              <tr key={line.journalLineId}>
                                <td className="text-muted">{line.lineNo}</td>
                                <td className="font-monospace">
                                  {line.accountCode}
                                </td>
                                <td>{line.accountName}</td>
                                <td className="text-muted">
                                  {line.description ?? '—'}
                                </td>
                                <td className="text-end font-monospace">
                                  {formatLineAmount(line.debitInBaseCurrency)}
                                </td>
                                <td className="text-end font-monospace">
                                  {formatLineAmount(line.creditInBaseCurrency)}
                                </td>
                              </tr>
                            ))}
                            {(selected.lines ?? []).length === 0 && (
                              <tr>
                                <td
                                  colSpan={6}
                                  className="text-center text-muted py-4"
                                >
                                  خطی برای این سند ثبت نشده است.
                                </td>
                              </tr>
                            )}
                          </tbody>
                          {(selected.lines ?? []).length > 0 && (
                            <tfoot className="table-light">
                              <tr>
                                <td colSpan={4} className="fw-semibold">
                                  جمع
                                </td>
                                <td className="text-end fw-semibold font-monospace">
                                  {formatAmount(
                                    selected.totalDebitInBaseCurrency,
                                  )}
                                </td>
                                <td className="text-end fw-semibold font-monospace">
                                  {formatAmount(
                                    selected.totalCreditInBaseCurrency,
                                  )}
                                </td>
                              </tr>
                            </tfoot>
                          )}
                        </table>
                      </div>
                    </>
                  )}
                </div>

                <div className="modal-footer border-0 pt-0">
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    onClick={closeDetail}
                  >
                    بستن
                  </button>
                </div>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  )
}

export default JournalEntriesPage
