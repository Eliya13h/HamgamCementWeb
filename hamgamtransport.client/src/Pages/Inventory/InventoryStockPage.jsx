import { useCallback, useMemo, useRef, useState } from 'react'
import Icon from '../../components/common/Icon'
import { useModalKeyboardShortcuts } from '../../hooks/useModalKeyboardShortcuts'
import DataTable from '../../lib/dataTableSetup'
import { inventoryStocksApi } from '../../services/inventoryApi'
import { dataTableLanguage, formatAmount, formatJalaliDate } from '../../components/common/CrudTablePage'

const columns = [
  { data: 'warehouseName', title: 'انبار' },
  { data: 'productCode', title: 'کد محصول' },
  { data: 'productName', title: 'نام محصول' },
  {
    data: 'displayQuantity',
    title: 'موجودی',
    orderable: false,
    className: 'text-end',
    render: (data, type, row) =>
      type === 'display'
        ? `${formatAmount(data)} ${row.displayUnit ?? ''}`
        : data,
  },
  {
    data: 'quantityInBase',
    title: 'معادل (کیلوگرم)',
    orderable: false,
    className: 'text-end',
    render: (data) => formatAmount(data),
  },
]

function InventoryStockPage() {
  const tableRef = useRef(null)
  const [loadError, setLoadError] = useState('')
  const [lots, setLots] = useState(null)
  const [lotsTitle, setLotsTitle] = useState('')

  const openLots = useCallback(async (row) => {
    try {
      const data = await inventoryStocksApi.fetchLots(row.warehouseId, row.productId)
      setLotsTitle(`${row.productName} — ${row.warehouseName}`)
      setLots(data)
    } catch (error) {
      setLoadError(error.message)
    }
  }, [])

  const closeLots = useCallback(() => setLots(null), [])

  useModalKeyboardShortcuts({
    open: Boolean(lots),
    onClose: closeLots,
  })

  const tableOptions = useMemo(
    () => ({
      processing: true,
      serverSide: true,
      ajax: inventoryStocksApi.createDataTableAjax(setLoadError),
      paging: true,
      searching: true,
      ordering: false,
      info: true,
      scrollX: false,
      autoWidth: false,
      responsive: true,
      stripeClasses: ['odd', 'even'],
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
        bottomEnd: { paging: { firstLast: true, previousNext: true, numbers: 5 } },
      },
      columns: [
        { data: 'rowNumber', name: 'rowNumber' },
        ...columns.map((col) => ({
          data: col.data,
          name: col.data,
          render: col.render,
        })),
        { data: null, name: 'actions', defaultContent: '' },
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
          targets: columns.length + 1,
          orderable: false,
          searchable: false,
          className: 'text-center all dt-actions-col',
          width: '72px',
        },
      ],
    }),
    [],
  )

  const actionSlots = useMemo(
    () => ({
      [columns.length + 1]: (_data, _type, row) => (
        <div className="dt-actions">
          <button type="button" className="dt-action-btn" title="لات‌ها / رهگیری تولید" onClick={() => openLots(row)}>
            <Icon name="route" />
          </button>
        </div>
      ),
    }),
    [openLots],
  )

  return (
    <div className="users-page">
      <div className="content-card card border-0 h-100">
        <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0">
          <h2 className="card-title mb-0">موجودی انبار</h2>
        </div>
        <div className="card-body card-body-table">
          {loadError && (
            <div className="alert alert-danger py-2 users-load-error mb-0">
              {loadError}
            </div>
          )}
          <div className="users-table-wrapper">
            <DataTable
              ref={tableRef}
              className="table table-hover w-100 align-middle"
              options={tableOptions}
              actionSlots={actionSlots}
            >
              <thead>
                <tr>
                  <th>#</th>
                  {columns.map((col) => (
                    <th key={col.data}>{col.title}</th>
                  ))}
                  <th></th>
                </tr>
              </thead>
            </DataTable>
          </div>
        </div>
      </div>

      {lots && (
        <div className="modal show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
          <div className="modal-dialog modal-lg modal-dialog-scrollable">
            <div className="modal-content">
              <div className="modal-header">
                <h5 className="modal-title">لات‌های موجود — {lotsTitle}</h5>
                <button type="button" className="btn-close" onClick={closeLots} />
              </div>
              <div className="modal-body">
                {lots.length === 0 ? (
                  <p className="text-muted mb-0">لات فعالی یافت نشد.</p>
                ) : (
                  <div className="table-responsive">
                    <table className="table table-sm align-middle">
                      <thead>
                        <tr>
                          <th>کد لات</th>
                          <th>باقیمانده</th>
                          <th>بهای واحد</th>
                          <th>تاریخ</th>
                          <th>بچ تولید</th>
                        </tr>
                      </thead>
                      <tbody>
                        {lots.map((lot) => (
                          <tr key={lot.inventoryLotId}>
                            <td>{lot.lotCode}</td>
                            <td>{formatAmount(lot.remainingQuantityInBase)}</td>
                            <td>{formatAmount(lot.unitCost)}</td>
                            <td>{formatJalaliDate(lot.receivedAt)}</td>
                            <td>{lot.productionBatchNumber || '—'}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

export default InventoryStockPage
