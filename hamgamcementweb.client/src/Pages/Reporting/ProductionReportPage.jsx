import { useCallback, useMemo, useRef, useState } from 'react'
import Icon from '../../components/common/Icon'
import DataTable from '../../lib/dataTableSetup'
import { productionBatchesApi } from '../../services/productionApi'
import { dataTableLanguage, formatAmount, formatJalaliDate } from '../Transport/CrudTablePage'

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
      order: [[3, 'desc']],
      pageLength: 15,
      language: dataTableLanguage,
      columns: [
        { data: 'rowNumber', name: 'rowNumber' },
        { data: 'batchNumber', name: 'batchNumber' },
        { data: 'outputWarehouseName', name: 'outputWarehouseName' },
        {
          data: 'productionDate',
          name: 'productionDate',
          render: (data) => formatJalaliDate(data),
        },
        {
          data: 'totalMaterialCostInBase',
          name: 'totalMaterialCostInBase',
          render: (data) => formatAmount(data),
          className: 'text-center',
        },
        {
          data: 'isTransferredToSales',
          name: 'isTransferredToSales',
          render: (data) =>
            data
              ? '<span class="badge badge-active">منتقل به فروش</span>'
              : '<span class="badge badge-inactive">در انبار تولید</span>',
          className: 'text-center',
        },
        { data: null, name: 'actions', defaultContent: '' },
      ],
      columnDefs: [
        { targets: 0, orderable: false, searchable: false, width: '56px', className: 'text-center' },
        {
          targets: 6,
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
      6: (_data, _type, row) =>
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
    <div className="content-card card border-0">
      <div className="card-body p-4">
        <h2 className="card-title mb-3">گزارش تولیدات</h2>
        {loadError && <div className="alert alert-danger">{loadError}</div>}
        <DataTable ref={tableRef} options={tableOptions} actionSlots={actionSlots} />

        {traceData && (
          <div className="modal show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
            <div className="modal-dialog modal-lg modal-dialog-scrollable">
              <div className="modal-content">
                <div className="modal-header">
                  <h5 className="modal-title">ردیابی — {traceData.batchNumber}</h5>
                  <button type="button" className="btn-close" onClick={() => setTraceData(null)} />
                </div>
                <div className="modal-body">
                  <p className="small text-muted mb-3">
                    {formatJalaliDate(traceData.productionDate)} — {traceData.outputWarehouseName}
                  </p>
                  <h6>مصرف</h6>
                  <ul className="list-group mb-3">
                    {(traceData.inputLines ?? []).map((line, i) => (
                      <li key={i} className="list-group-item">{line.productName} — {formatAmount(line.quantity)} {line.meaurmentName}</li>
                    ))}
                  </ul>
                  <h6>تولید</h6>
                  <ul className="list-group">
                    {(traceData.outputLines ?? []).map((line, i) => (
                      <li key={i} className="list-group-item">{line.productName} — {formatAmount(line.quantity)} {line.meaurmentName}</li>
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
