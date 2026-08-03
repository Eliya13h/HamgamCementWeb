import { useCallback, useMemo, useRef, useState } from 'react'
import Icon from '../../components/common/Icon'
import { useModalKeyboardShortcuts } from '../../hooks/useModalKeyboardShortcuts'
import DataTable from '../../lib/dataTableSetup'
import { PRODUCTION_COST_TYPE_OPTIONS, productionBatchesApi } from '../../services/productionApi'
import { dataTableLanguage, formatAmount, formatJalaliDate } from '../Transport/CrudTablePage'

function costTypeLabel(value) {
  return PRODUCTION_COST_TYPE_OPTIONS.find((o) => o.value === Number(value))?.label ?? value
}

const reportColumns = [
  { data: 'batchNumber', title: 'شماره سند' },
  { data: 'formulaName', title: 'فرمول' },
  { data: 'outputWarehouseName', title: 'انبار' },
  { data: 'productionDate', title: 'تاریخ' },
  { data: 'statusLabel', title: 'وضعیت' },
  { data: 'totalCostInBase', title: 'بهای تمام‌شده' },
]

function ProductionReportPage() {
  const tableRef = useRef(null)
  const [loadError, setLoadError] = useState('')
  const [traceData, setTraceData] = useState(null)

  const openTrace = useCallback(async (row) => {
    try {
      const trace = await productionBatchesApi.trace(row.productionBatchId)
      setTraceData(trace)
    } catch (error) {
      setLoadError(error.message)
    }
  }, [])

  const closeTrace = useCallback(() => setTraceData(null), [])

  useModalKeyboardShortcuts({
    open: Boolean(traceData),
    onClose: closeTrace,
  })

  const tableOptions = useMemo(
    () => ({
      processing: true,
      serverSide: true,
      ajax: productionBatchesApi.createDataTableAjax(setLoadError),
      paging: true,
      searching: true,
      ordering: true,
      info: true,
      scrollX: true,
      autoWidth: false,
      responsive: true,
      stripeClasses: ['odd', 'even'],
      order: [[4, 'desc']],
      pageLength: 15,
      language: dataTableLanguage,
      columns: [
        { data: 'rowNumber', name: 'rowNumber' },
        { data: 'batchNumber', name: 'batchNumber', title: 'شماره سند' },
        { data: 'formulaName', name: 'formulaName', defaultContent: '—', title: 'فرمول' },
        { data: 'outputWarehouseName', name: 'outputWarehouseName', title: 'انبار' },
        {
          data: 'productionDate',
          name: 'productionDate',
          title: 'تاریخ',
          render: (data) => formatJalaliDate(data),
        },
        {
          data: 'statusLabel',
          name: 'status',
          title: 'وضعیت',
          className: 'text-center',
        },
        {
          data: 'totalCostInBase',
          name: 'totalCostInBase',
          title: 'بهای تمام‌شده',
          render: (data, _t, row) =>
            formatAmount(data || row.totalMaterialCostInBase),
          className: 'text-center',
        },
        { data: null, name: 'actions', defaultContent: '', title: 'عملیات' },
      ],
      columnDefs: [
        { targets: 0, orderable: false, searchable: false, width: '56px', className: 'text-center' },
        {
          targets: 7,
          orderable: false,
          searchable: false,
          className: 'text-center all dt-actions-col',
          width: '80px',
        },
      ],
    }),
    [],
  )

  const actionSlots = useMemo(
    () => ({
      7: (_data, _type, row) =>
        row.isPosted ? (
          <div className="dt-actions">
            <button type="button" className="dt-action-btn" title="ردیابی" onClick={() => openTrace(row)}>
              <Icon name="route" />
            </button>
          </div>
        ) : null,
    }),
    [openTrace],
  )

  return (
    <div className="content-card card border-0 production-page">
      <div className="card-body p-4">
        <h2 className="card-title mb-3">گزارش تولیدات</h2>
        {loadError && <div className="alert alert-danger">{loadError}</div>}
        <div className="users-table-wrapper">
          <DataTable ref={tableRef} className="table table-hover w-100 align-middle" options={tableOptions} slots={actionSlots}>
            <thead>
              <tr>
                <th>#</th>
                {reportColumns.map((col) => (
                  <th key={col.data}>{col.title}</th>
                ))}
                <th>عملیات</th>
              </tr>
            </thead>
          </DataTable>
        </div>

        {traceData && (
          <div className="modal show d-block production-modal" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
            <div className="modal-dialog modal-lg modal-dialog-scrollable">
              <div className="modal-content">
                <div className="modal-header">
                  <h5 className="modal-title">ردیابی — {traceData.batchNumber}</h5>
                  <button type="button" className="btn-close" onClick={closeTrace} />
                </div>
                <div className="modal-body">
                  <p className="small text-muted mb-2">
                    {formatJalaliDate(traceData.productionDate)} — {traceData.outputWarehouseName}
                  </p>
                  <p className="mb-3">
                    بهای تمام‌شده: {formatAmount(traceData.totalCostInBase)} — مواد:{' '}
                    {formatAmount(traceData.totalMaterialCostInBase)} — تبدیل:{' '}
                    {formatAmount(traceData.totalConversionCostInBase)}
                  </p>

                  <h6>مصرف مواد</h6>
                  <ul className="list-group mb-3">
                    {(traceData.inputLines ?? []).map((line, i) => (
                      <li key={i} className="list-group-item">
                        {line.productName} — {formatAmount(line.quantity)} {line.meaurmentName} (بهای مواد:{' '}
                        {formatAmount(line.materialCostInBase)})
                      </li>
                    ))}
                  </ul>

                  <h6>هزینه‌ها</h6>
                  <ul className="list-group mb-3">
                    {(traceData.costLines ?? []).length === 0 && (
                      <li className="list-group-item text-muted">هزینه ثبت نشده</li>
                    )}
                    {(traceData.costLines ?? []).map((line, i) => (
                      <li key={i} className="list-group-item">
                        {costTypeLabel(line.costType)}
                        {line.description ? ` — ${line.description}` : ''} — {formatAmount(line.amount)}
                      </li>
                    ))}
                  </ul>

                  <h6>خروجی و لات‌ها</h6>
                  <ul className="list-group mb-3">
                    {(traceData.inventoryLots ?? []).map((lot, i) => (
                      <li key={i} className="list-group-item">
                        {lot.lotCode} — {lot.productName} — باقیمانده:{' '}
                        {formatAmount(lot.remainingQuantityInBase)} — بهای واحد:{' '}
                        {formatAmount(lot.unitCost)}
                      </li>
                    ))}
                  </ul>

                  <h6>لات‌های مصرف‌شده (FIFO)</h6>
                  <ul className="list-group mb-3">
                    {(traceData.consumedLots ?? []).map((lot, i) => (
                      <li key={i} className="list-group-item">
                        {lot.lotCode} — {lot.productName} — {formatAmount(lot.quantityInBase)} (بهای:{' '}
                        {formatAmount(lot.lineCostInBase)})
                      </li>
                    ))}
                  </ul>

                  <h6>فروش از این تولید</h6>
                  <ul className="list-group">
                    {(traceData.sales ?? []).length === 0 && (
                      <li className="list-group-item text-muted">هنوز فروشی ثبت نشده</li>
                    )}
                    {(traceData.sales ?? []).map((sale, i) => (
                      <li key={i} className="list-group-item">
                        {sale.invoiceNumber} — {formatJalaliDate(sale.invoiceDate)} —{' '}
                        {formatAmount(sale.quantityInBase)} از لات {sale.lotCode}
                      </li>
                    ))}
                  </ul>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

export default ProductionReportPage
