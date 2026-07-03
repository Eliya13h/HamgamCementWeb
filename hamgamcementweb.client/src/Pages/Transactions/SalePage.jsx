import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import Icon from '../../components/common/Icon'
import AmountDisplay from '../../components/common/AmountDisplay'
import AmountField from '../../components/common/AmountField'
import JalaliDateField from '../../components/common/JalaliDateField'
import PrefixNumberField from '../../components/common/PrefixNumberField'
import SearchableSelect from '../../components/common/SearchableSelect'
import { useModalKeyboardShortcuts } from '../../hooks/useModalKeyboardShortcuts'
import { todayGregorianIso } from '../../lib/afghanSolarCalendar'
import DataTable from '../../lib/dataTableSetup'
import { fetchBaseCurrency, fetchCurrencyRates } from '../../services/currenciesApi'
import { usePageCrud } from '../../permissions/usePageCrud'
import { fetchWarehouseOptions } from '../../services/inventoryApi'
import { fetchMeaurmentOptions, fetchProductOptions } from '../../services/productsApi'
import { fetchCurrencyOptions } from '../../services/transportApi'
import {
  INVOICE_STATUSES,
  INVOICE_DOCUMENT_TYPE,
  buildSalePayload,
  calcLineTotals,
  convertAmountFromBase,
  convertAmountToBase,
  fetchCurrencyRateAt,
  getCurrencyRateToBase,
  fetchCustomerOptions,
  getSaleInvoicePrintUrl,
  saleInvoicesApi,
  renderInvoiceDocumentTypeBadge,
  sumTotals,
} from '../../services/transactionsApi'
import InvoiceReturnModal from '../../components/transactions/InvoiceReturnModal'
import { dataTableLanguage, formatAmount, formatJalaliDate } from '../Transport/CrudTablePage'
import '../../styles/purchase-invoice-lines.css'

const emptyHeader = {
  customerId: '',
  warehouseId: '',
  invoiceDate: '',
  status: '1',
  currencyId: '',
  description: '',
  paidAmount: '',
}

const emptyLine = {
  salesItemId: null,
  productId: '',
  meaurmentId: '',
  quantity: '',
  unitPrice: '',
  unitPriceInBase: '',
}

function SalePage() {
  const tableRef = useRef(null)
  const formRef = useRef(null)
  const { canCreate, canEdit, canDelete } = usePageCrud('/transactions/sale')
  const [loadError, setLoadError] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editId, setEditId] = useState(null)
  const [viewPosted, setViewPosted] = useState(false)
  const [deleteRow, setDeleteRow] = useState(null)
  const [formError, setFormError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [header, setHeader] = useState(emptyHeader)
  const [lines, setLines] = useState([{ ...emptyLine }])
  const [customers, setCustomers] = useState([])
  const [warehouses, setWarehouses] = useState([])
  const [products, setProducts] = useState([])
  const [meaurments, setMeaurments] = useState([])
  const [currencies, setCurrencies] = useState([])
  const [rateSnapshot, setRateSnapshot] = useState(null)
  const [exchangeRate, setExchangeRate] = useState('')
  const [exchangeRateTouched, setExchangeRateTouched] = useState(false)
  const [paidAmountTouched, setPaidAmountTouched] = useState(false)
  const [baseCurrencyId, setBaseCurrencyId] = useState('')
  const [currencyRates, setCurrencyRates] = useState({})
  const [baseCurrencySymbol, setBaseCurrencySymbol] = useState('')
  const [postedTotals, setPostedTotals] = useState(null)
  const [invoiceCodePreview, setInvoiceCodePreview] = useState('')
  const [documentType, setDocumentType] = useState(INVOICE_DOCUMENT_TYPE.Invoice)
  const [referenceInvoiceNumber, setReferenceInvoiceNumber] = useState('')
  const [pastReturns, setPastReturns] = useState([])
  const [returnSource, setReturnSource] = useState(null)

  const currencySymbolById = useMemo(
    () => Object.fromEntries(currencies.map((c) => [String(c.value), c.symbol ?? ''])),
    [currencies],
  )

  const invoiceCurrencySymbol = useMemo(() => {
    return currencySymbolById[String(header.currencyId)] ?? ''
  }, [currencySymbolById, header.currencyId])

  const meaurmentSymbol = useCallback(
    (meaurmentId) => {
      const unit = meaurments.find((m) => String(m.value) === String(meaurmentId))
      return unit?.symbol || unit?.label || ''
    },
    [meaurments],
  )

  useEffect(() => {
    fetchCustomerOptions().then(setCustomers).catch(() => setCustomers([]))
    fetchWarehouseOptions().then(setWarehouses).catch(() => setWarehouses([]))
    fetchProductOptions().then(setProducts).catch(() => setProducts([]))
    fetchMeaurmentOptions().then(setMeaurments).catch(() => setMeaurments([]))
    fetchCurrencyOptions().then(setCurrencies).catch(() => setCurrencies([]))
    fetchCurrencyRates()
      .then((data) => {
        setBaseCurrencyId(String(data?.baseCurrencyId ?? ''))
        const map = {}
        for (const row of data?.rates ?? []) {
          map[String(row.currencyId)] = row.baseUnitsPerUnit
        }
        setCurrencyRates(map)
      })
      .catch(() => {
        setBaseCurrencyId('')
        setCurrencyRates({})
      })
    fetchBaseCurrency()
      .then((base) => setBaseCurrencySymbol(base?.symbol ?? ''))
      .catch(() => setBaseCurrencySymbol(''))
  }, [])

  useEffect(() => {
    if (!header.currencyId) {
      setRateSnapshot(null)
      setExchangeRate('')
      return
    }
    fetchCurrencyRateAt(header.currencyId, header.invoiceDate || undefined)
      .then((snapshot) => {
        setRateSnapshot(snapshot)
        const rate = snapshot.isBaseCurrency ? '1' : String(snapshot.baseUnitsPerUnit ?? '')
        setExchangeRate(rate)
        if (!exchangeRateTouched) {
          setLines((prev) =>
            prev.map((line) => {
              if (line.unitPriceInBase == null || line.unitPriceInBase === '') return line
              return {
                ...line,
                unitPrice: convertAmountFromBase(
                  line.unitPriceInBase,
                  header.currencyId,
                  baseCurrencyId,
                  rate,
                ),
              }
            }),
          )
        }
      })
      .catch(() => {
        setRateSnapshot(null)
        setExchangeRate('')
      })
  }, [header.currencyId, header.invoiceDate, baseCurrencyId, exchangeRateTouched])

  const isNonBaseCurrency = Boolean(rateSnapshot && !rateSnapshot.isBaseCurrency)

  const computedLines = useMemo(
    () =>
      calcLineTotals(
        lines,
        rateSnapshot,
        exchangeRate,
        meaurments,
        baseCurrencyId,
        header.currencyId,
      ),
    [lines, rateSnapshot, exchangeRate, meaurments, baseCurrencyId, header.currencyId],
  )
  const totals = useMemo(() => sumTotals(computedLines), [computedLines])

  const paidAmountNumeric = Number(header.paidAmount) || 0
  const remainingAmount = Math.max(0, totals.total - paidAmountNumeric)
  const isCashInvoice = totals.total > 0 && paidAmountNumeric >= totals.total
  const isInvoiceStatus = String(header.status) === '4'
  const isQuotationStatus = String(header.status) === '1'
  const showPaymentField = !isQuotationStatus
  const showReturnedQty = viewPosted && documentType === INVOICE_DOCUMENT_TYPE.Invoice

  useEffect(() => {
    if (!showPaymentField) {
      setHeader((prev) => (prev.paidAmount === '' || prev.paidAmount === '0' ? prev : { ...prev, paidAmount: '' }))
      return
    }
    if (paidAmountTouched) return
    setHeader((prev) => ({
      ...prev,
      paidAmount: totals.total > 0 ? String(totals.total) : '',
    }))
  }, [totals.total, paidAmountTouched, showPaymentField])

  const reloadTable = useCallback(() => {
    tableRef.current?.dt()?.ajax.reload(null, false)
  }, [])

  const openPrint = useCallback((saleInvoiceId) => {
    if (!saleInvoiceId) return
    window.open(getSaleInvoicePrintUrl(saleInvoiceId), '_blank', 'noopener,noreferrer')
  }, [])

  const closeModals = useCallback(() => {
    setShowForm(false)
    setEditId(null)
    setViewPosted(false)
    setDeleteRow(null)
    setFormError('')
    setSubmitting(false)
    setExchangeRate('')
    setExchangeRateTouched(false)
    setPaidAmountTouched(false)
    setPostedTotals(null)
    setInvoiceCodePreview('')
    setDocumentType(INVOICE_DOCUMENT_TYPE.Invoice)
    setReferenceInvoiceNumber('')
    setPastReturns([])
    setReturnSource(null)
  }, [])

  const openCreate = useCallback(async () => {
    setFormError('')
    setExchangeRateTouched(false)
    setPaidAmountTouched(false)
    setExchangeRate('')

    let defaultCurrencyId = ''
    let defaultWarehouseId = ''
    try {
      const [base, ratesData, warehouseList] = await Promise.all([
        fetchBaseCurrency(),
        fetchCurrencyRates().catch(() => null),
        fetchWarehouseOptions().catch(() => []),
      ])
      if (base?.currencyID) {
        defaultCurrencyId = String(base.currencyID)
        setBaseCurrencyId(defaultCurrencyId)
        setBaseCurrencySymbol(base.symbol ?? '')
      }
      if (ratesData) {
        const map = {}
        for (const row of ratesData.rates ?? []) {
          map[String(row.currencyId)] = row.baseUnitsPerUnit
        }
        setCurrencyRates(map)
      }
      if (warehouseList.length > 0) {
        setWarehouses(warehouseList)
        defaultWarehouseId = String(warehouseList[0].value)
      }
    } catch {
      defaultCurrencyId = ''
    }

    if (!defaultWarehouseId && warehouses.length > 0) {
      defaultWarehouseId = String(warehouses[0].value)
    }

    setHeader({
      ...emptyHeader,
      invoiceDate: todayGregorianIso(),
      currencyId: defaultCurrencyId,
      warehouseId: defaultWarehouseId,
    })
    setLines([{ ...emptyLine }])
    setEditId(null)
    setViewPosted(false)
    setPostedTotals(null)
    setShowForm(true)
  }, [warehouses])

  const openEdit = useCallback(async (row, readOnly = false) => {
    setFormError('')
    try {
      const invoice = await saleInvoicesApi.getById(row.saleInvoiceId)
      setInvoiceCodePreview(invoice.invoiceNumber ?? row.invoiceNumber ?? '')
      const invoiceRate = invoice.baseUnitsPerUnitAtTransaction || 1
      setHeader({
        customerId: invoice.customerId,
        warehouseId: invoice.warehouseId,
        invoiceDate: String(invoice.invoiceDate).slice(0, 10),
        status: String(invoice.status),
        currencyId: invoice.currencyId,
        description: invoice.description ?? '',
        paidAmount: invoice.paidAmount ?? invoice.totalAmount ?? '',
      })
      setExchangeRate(
        invoice.baseUnitsPerUnitAtTransaction != null
          ? String(invoice.baseUnitsPerUnitAtTransaction)
          : '',
      )
      setLines(
        (invoice.items ?? []).map((item) => {
          const qtyBase = Number(item.quantityInBase) || 0
          const unitPriceInBase =
            qtyBase > 0
              ? Number(item.lineTotalInBaseCurrency) / qtyBase
              : convertAmountToBase(
                  item.unitPrice,
                  invoice.currencyId,
                  invoice.baseCurrencyId,
                  invoiceRate,
                )
          return {
            salesItemId: item.salesItemId,
            productId: item.productId,
            meaurmentId: item.meaurmentId,
            quantity: item.quantity,
            returnedQuantity: Number(item.returnedQuantity) || 0,
            unitPrice: item.unitPrice,
            unitPriceInBase: unitPriceInBase || '',
            lineCostInBaseCurrency: item.lineCostInBaseCurrency,
            lineProfitInBaseCurrency: item.lineProfitInBaseCurrency,
            lotAllocations: item.lotAllocations,
          }
        }),
      )
      setEditId(invoice.saleInvoiceId)
      setDocumentType(invoice.documentType ?? INVOICE_DOCUMENT_TYPE.Invoice)
      setReferenceInvoiceNumber(invoice.referenceInvoiceNumber ?? '')
      setViewPosted(readOnly || invoice.isPosted)
      if (
        (readOnly || invoice.isPosted) &&
        (invoice.documentType ?? INVOICE_DOCUMENT_TYPE.Invoice) === INVOICE_DOCUMENT_TYPE.Invoice
      ) {
        try {
          const history = await saleInvoicesApi.fetchReturns(invoice.saleInvoiceId)
          setPastReturns(history ?? [])
        } catch {
          setPastReturns([])
        }
      } else {
        setPastReturns([])
      }
      if (invoice.isPosted) {
        setPostedTotals({
          totalCostInBaseCurrency: invoice.totalCostInBaseCurrency,
          totalProfitInBaseCurrency: invoice.totalProfitInBaseCurrency,
        })
      } else {
        setPostedTotals(null)
      }
      setShowForm(true)
    } catch (error) {
      setLoadError(error.message)
    }
  }, [])

  const handleHeaderChange = (name, value) => {
    setHeader((prev) => ({ ...prev, [name]: value }))
  }

  const handleCurrencyChange = (newCurrencyId) => {
    const oldCurrencyId = header.currencyId
    if (oldCurrencyId && newCurrencyId && oldCurrencyId !== newCurrencyId) {
      const oldRate = getCurrencyRateToBase(
        oldCurrencyId,
        baseCurrencyId,
        currencyRates,
        exchangeRate,
      )
      const newRate = getCurrencyRateToBase(newCurrencyId, baseCurrencyId, currencyRates)

      setLines((prev) =>
        prev.map((line) => {
          const priceInBase =
            line.unitPriceInBase != null && line.unitPriceInBase !== ''
              ? Number(line.unitPriceInBase)
              : convertAmountToBase(line.unitPrice, oldCurrencyId, baseCurrencyId, oldRate)

          if (!priceInBase && line.unitPrice === '') {
            return { ...line }
          }

          return {
            ...line,
            unitPriceInBase: priceInBase || line.unitPriceInBase || '',
            unitPrice: convertAmountFromBase(
              priceInBase,
              newCurrencyId,
              baseCurrencyId,
              newRate,
            ),
          }
        }),
      )
      setExchangeRate(String(newCurrencyId) === String(baseCurrencyId) ? '1' : String(newRate))
      setExchangeRateTouched(false)
    }
    handleHeaderChange('currencyId', newCurrencyId)
  }

  const handleExchangeRateChange = (value) => {
    setExchangeRateTouched(true)
    setExchangeRate(value)
    setLines((prev) =>
      prev.map((line) => {
        if (line.unitPriceInBase == null || line.unitPriceInBase === '') return line
        return {
          ...line,
          unitPrice: convertAmountFromBase(
            line.unitPriceInBase,
            header.currencyId,
            baseCurrencyId,
            value,
          ),
        }
      }),
    )
  }

  const handleLineChange = (index, name, value) => {
    if (name === 'unitPrice') {
      const rate = getCurrencyRateToBase(
        header.currencyId,
        baseCurrencyId,
        currencyRates,
        exchangeRate,
      )
      setLines((prev) =>
        prev.map((line, i) => {
          if (i !== index) return line
          return {
            ...line,
            unitPrice: value,
            unitPriceInBase:
              value === ''
                ? ''
                : convertAmountToBase(value, header.currencyId, baseCurrencyId, rate),
          }
        }),
      )
      return
    }

    setLines((prev) =>
      prev.map((line, i) => (i === index ? { ...line, [name]: value } : line)),
    )
  }

  const handleMeaurmentChange = (index, newMeaurmentId) => {
    handleLineChange(index, 'meaurmentId', newMeaurmentId)
  }

  const handleProductChange = (index, productId) => {
    const product = products.find((p) => String(p.value) === String(productId))
    const rate = getCurrencyRateToBase(
      header.currencyId,
      baseCurrencyId,
      currencyRates,
      exchangeRate,
    )
    const priceInBase = product?.defaultSalePrice ?? ''
    setLines((prev) =>
      prev.map((line, i) => {
        if (i !== index) return line
        return {
          ...line,
          productId,
          meaurmentId: product?.defaultMeaurmentId ?? '',
          unitPriceInBase: priceInBase,
          unitPrice:
            priceInBase === ''
              ? ''
              : convertAmountFromBase(priceInBase, header.currencyId, baseCurrencyId, rate),
        }
      }),
    )
  }

  const meaurmentsForProduct = (productId) => {
    const product = products.find((p) => String(p.value) === String(productId))
    if (!product?.baseMeaurmentId) return meaurments
    return meaurments.filter(
      (m) =>
        m.baseMeaurmentId === product.baseMeaurmentId ||
        m.value === product.baseMeaurmentId,
    )
  }

  const addLine = () => setLines((prev) => [...prev, { ...emptyLine }])
  const removeLine = (index) =>
    setLines((prev) => (prev.length > 1 ? prev.filter((_, i) => i !== index) : prev))

  const handleSubmit = async (event) => {
    event.preventDefault()
    if (viewPosted) return

    if (showPaymentField) {
      const paid = Number(header.paidAmount) || 0
      if (paid < 0) {
        setFormError('مبلغ دریافت‌شده نمی‌تواند منفی باشد.')
        return
      }
      if (totals.total > 0 && paid > totals.total) {
        setFormError('مبلغ دریافت‌شده نمی‌تواند بیشتر از جمع فاکتور باشد.')
        return
      }
    }

    setSubmitting(true)
    setFormError('')

    try {
      const payload = buildSalePayload(header, lines, exchangeRate)
      if (editId) {
        await saleInvoicesApi.update(editId, payload)
      } else {
        await saleInvoicesApi.create(payload)
      }
      closeModals()
      reloadTable()
    } catch (error) {
      setFormError(error.message)
      setSubmitting(false)
    }
  }

  const handlePost = async () => {
    if (!editId) return
    setSubmitting(true)
    setFormError('')
    try {
      await saleInvoicesApi.post(editId)
      closeModals()
      reloadTable()
    } catch (error) {
      setFormError(error.message)
      setSubmitting(false)
    }
  }

  const handleDeleteConfirm = async () => {
    if (!deleteRow) return
    setSubmitting(true)
    setFormError('')
    try {
      await saleInvoicesApi.remove(deleteRow.saleInvoiceId)
      closeModals()
      reloadTable()
    } catch (error) {
      setFormError(error.message)
      setSubmitting(false)
    }
  }

  const triggerSave = useCallback(() => {
    if (!submitting && !viewPosted) {
      formRef.current?.requestSubmit()
    }
  }, [submitting, viewPosted])

  useModalKeyboardShortcuts({
    open: showForm,
    onClose: closeModals,
    onSave: !viewPosted ? triggerSave : undefined,
    formRef,
  })

  const openReturn = useCallback((row) => {
    setReturnSource({
      saleInvoiceId: row.saleInvoiceId,
      invoiceNumber: row.invoiceNumber,
    })
  }, [])

  const handleReturnSuccess = useCallback(() => {
    reloadTable()
  }, [reloadTable])

  const tableOptions = useMemo(
    () => ({
      processing: true,
      serverSide: true,
      ajax: saleInvoicesApi.createDataTableAjax(setLoadError),
      paging: true,
      searching: true,
      ordering: true,
      info: true,
      scrollX: true,
      autoWidth: false,
      responsive: true,
      stripeClasses: ['odd', 'even'],
      order: [[5, 'desc']],
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
        { data: 'invoiceNumber', name: 'invoiceNumber' },
        {
          data: 'documentType',
          name: 'documentType',
          render: (data, _type, row) => {
            const badge = renderInvoiceDocumentTypeBadge(data)
            if (row.referenceInvoiceNumber) {
              return `${badge}<div class="small text-muted mt-1">مبدأ: ${row.referenceInvoiceNumber}</div>`
            }
            return badge
          },
        },
        { data: 'customerName', name: 'customerName' },
        { data: 'warehouseName', name: 'warehouseName' },
        {
          data: 'invoiceDate',
          name: 'invoiceDate',
          render: (data) => formatJalaliDate(data),
        },
        {
          data: 'totalAmount',
          name: 'totalAmount',
          render: (data) => formatAmount(data),
        },
        {
          data: 'totalProfitInBaseCurrency',
          name: 'totalProfitInBaseCurrency',
          render: (data) => formatAmount(data),
        },
        {
          data: 'isPosted',
          name: 'isPosted',
          render: (data) =>
            data
              ? '<span class="badge badge-active">ثبت‌شده</span>'
              : '<span class="badge badge-inactive">پیش‌نویس</span>',
        },
        { data: null, name: 'actions', defaultContent: '' },
      ],
      columnDefs: [
        { targets: 0, orderable: false, searchable: false, width: '56px', className: 'text-center' },
        { targets: [3, 4, 8], orderable: false },
        { targets: [2, 5, 6, 7, 8], className: 'text-center' },
        {
          targets: 9,
          orderable: false,
          searchable: false,
          className: 'text-center all dt-actions-col',
          width: '196px',
        },
      ],
    }),
    [],
  )

  const actionSlots = useMemo(
    () => ({
      9: (_data, _type, row) => (
        <div className="dt-actions">
          {(canEdit || row.isPosted) && (
            <button
              type="button"
              className="dt-action-btn"
              title={row.isPosted ? 'مشاهده' : 'ویرایش'}
              onClick={() => openEdit(row, row.isPosted)}
            >
              <Icon name={row.isPosted ? 'eye' : 'edit'} />
            </button>
          )}
          {canCreate &&
            row.isPosted &&
            row.documentType === INVOICE_DOCUMENT_TYPE.Invoice && (
              <button
                type="button"
                className="dt-action-btn"
                title="برگشت از فروش"
                onClick={() => openReturn(row)}
              >
                <Icon name="rotate-left" />
              </button>
            )}
          {canDelete && !row.isPosted && (
            <button
              type="button"
              className="dt-action-btn btn-delete"
              title="حذف"
              onClick={() => setDeleteRow(row)}
            >
              <Icon name="trash" />
            </button>
          )}
          <button
            type="button"
            className="dt-action-btn"
            title="چاپ"
            onClick={() => openPrint(row.saleInvoiceId)}
          >
            <Icon name="print" />
          </button>
        </div>
      ),
    }),
    [openEdit, openPrint, openReturn, canEdit, canDelete, canCreate],
  )

  return (
    <div className="users-page">
      <div className="content-card card border-0 h-100">
        <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between gap-3 flex-wrap">
          <h2 className="card-title mb-0">فاکتورهای فروش</h2>
          {canCreate && (
            <button
              type="button"
              className="btn btn-sm btn-accent btn-users-new d-inline-flex align-items-center gap-2"
              onClick={openCreate}
            >
              <Icon name="plus" />
              <span>فاکتور فروش جدید</span>
            </button>
          )}
        </div>

        <div className="card-body card-body-table">
          {loadError && <div className="alert alert-danger py-2 mb-0">{loadError}</div>}
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
                  <th>نوع</th>
                  <th>مشتری</th>
                  <th>انبار</th>
                  <th>تاریخ</th>
                  <th>جمع فروش</th>
                  <th>سود FIFO (پایه)</th>
                  <th>وضعیت</th>
                  <th>عملیات</th>
                </tr>
              </thead>
            </DataTable>
          </div>
        </div>
      </div>

      {showForm && (
        <>
          <div className="modal-backdrop show users-modal-backdrop" onClick={closeModals} />
          <div className="modal show d-block users-modal" tabIndex="-1" data-bs-focus="false">
            <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable modal-xl">
              <form ref={formRef} className="modal-content" onSubmit={handleSubmit}>
                <div className="modal-header">
                  <h5 className="modal-title">
                    {viewPosted
                      ? documentType === INVOICE_DOCUMENT_TYPE.SaleReturn
                        ? `مشاهده برگشت از فروش${invoiceCodePreview ? ` — ${invoiceCodePreview}` : ''}`
                        : `مشاهده فاکتور فروش${invoiceCodePreview ? ` — ${invoiceCodePreview}` : ''}`
                      : editId
                        ? `ویرایش فاکتور فروش${invoiceCodePreview ? ` — ${invoiceCodePreview}` : ''}`
                        : 'فاکتور فروش جدید'}
                  </h5>
                  <button type="button" className="btn-close" aria-label="بستن" onClick={closeModals} />
                </div>
                <div className="modal-body">
                  {formError && <div className="alert alert-danger py-2">{formError}</div>}
                  {documentType === INVOICE_DOCUMENT_TYPE.SaleReturn && referenceInvoiceNumber && (
                    <div className="alert alert-secondary py-2">
                      برگشت از فاکتور مبدأ: <strong>{referenceInvoiceNumber}</strong>
                    </div>
                  )}
                  {viewPosted &&
                    documentType === INVOICE_DOCUMENT_TYPE.Invoice &&
                    pastReturns.length > 0 && (
                      <div className="mb-3">
                        <h6 className="mb-2">سوابق برگشت این فاکتور</h6>
                        <div className="table-responsive">
                          <table className="table table-sm table-bordered mb-0">
                            <thead>
                              <tr>
                                <th>شماره برگشت</th>
                                <th>تاریخ</th>
                                <th>مبلغ</th>
                              </tr>
                            </thead>
                            <tbody>
                              {pastReturns.map((row) => (
                                <tr key={row.invoiceId}>
                                  <td>{row.invoiceNumber}</td>
                                  <td>{formatJalaliDate(row.invoiceDate)}</td>
                                  <td>{formatAmount(row.totalAmount)}</td>
                                </tr>
                              ))}
                            </tbody>
                          </table>
                        </div>
                      </div>
                    )}

                  <div className="row g-3 mb-3">
                    <div className="col-md-3">
                      <label className="form-label">مشتری</label>
                      <SearchableSelect
                        options={customers}
                        value={header.customerId}
                        onChange={(next) => handleHeaderChange('customerId', next)}
                        placeholder="انتخاب کنید..."
                        searchPlaceholder="جستجوی مشتری..."
                        required
                        disabled={viewPosted}
                      />
                    </div>
                    <div className="col-md-3">
                      <label className="form-label">انبار</label>
                      <select
                        className="form-select"
                        value={header.warehouseId}
                        required
                        disabled={viewPosted}
                        onChange={(e) => handleHeaderChange('warehouseId', e.target.value)}
                      >
                        <option value="">انتخاب کنید...</option>
                        {warehouses.map((o) => (
                          <option key={o.value} value={o.value}>
                            {o.label}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div className="col-md-3">
                      <label className="form-label">تاریخ (شمسی)</label>
                      <JalaliDateField
                        value={header.invoiceDate}
                        onChange={(next) => handleHeaderChange('invoiceDate', next)}
                        required
                        disabled={viewPosted}
                      />
                    </div>
                    <div className="col-md-3">
                      <label className="form-label">ارز فاکتور</label>
                      <select
                        className="form-select"
                        value={header.currencyId}
                        required
                        disabled={viewPosted}
                        onChange={(e) => handleCurrencyChange(e.target.value)}
                      >
                        <option value="">انتخاب کنید...</option>
                        {currencies.map((o) => (
                          <option key={o.value} value={o.value}>
                            {o.label}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div className="col-md-3">
                      <label className="form-label">وضعیت</label>
                      <select
                        className="form-select"
                        value={header.status}
                        disabled={viewPosted}
                        onChange={(e) => handleHeaderChange('status', e.target.value)}
                      >
                        {INVOICE_STATUSES.map((s) => (
                          <option key={s.value} value={s.value}>
                            {s.label}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div className="col-md-3">
                      <label className="form-label">نرخ به ارز پایه</label>
                      {isNonBaseCurrency ? (
                        <input
                          type="number"
                          min="0"
                          step="any"
                          className="form-control"
                          value={exchangeRate}
                          required
                          disabled={viewPosted}
                          onChange={(e) => handleExchangeRateChange(e.target.value)}
                        />
                      ) : (
                        <input
                          type="text"
                          className="form-control"
                          readOnly
                          value={
                            rateSnapshot
                              ? rateSnapshot.isBaseCurrency
                                ? 'ارز پایه (۱:۱)'
                                : formatAmount(rateSnapshot.baseUnitsPerUnit)
                              : header.currencyId
                                ? 'در حال بارگذاری...'
                                : '—'
                          }
                        />
                      )}
                    </div>
                    <div className="col-md-3">
                      <label className="form-label">کل فاکتور</label>
                      <AmountField
                        value={totals.total}
                        onChange={() => {}}
                        symbol={invoiceCurrencySymbol}
                        readOnly
                      />
                    </div>
                    {showPaymentField && (
                      <div className="col-md-3">
                        <label className="form-label">مقدار دریافت‌شده</label>
                        <AmountField
                          value={header.paidAmount}
                          onChange={(next) => {
                            setPaidAmountTouched(true)
                            handleHeaderChange('paidAmount', next)
                          }}
                          symbol={invoiceCurrencySymbol}
                          required
                          disabled={viewPosted}
                          min="0"
                          max={totals.total > 0 ? String(totals.total) : undefined}
                        />
                        {totals.total > 0 && (
                          <small className={`text-muted d-block mt-1${isCashInvoice ? '' : ' text-warning'}`}>
                            {isCashInvoice ? (
                              'فاکتور نقدی — کل مبلغ دریافت می‌شود'
                            ) : (
                              <>
                                فاکتور نسیه — مانده:{' '}
                                <AmountDisplay value={remainingAmount} symbol={invoiceCurrencySymbol} />
                              </>
                            )}
                          </small>
                        )}
                      </div>
                    )}
                    <div className="col-12">
                      <label className="form-label">توضیحات</label>
                      <input
                        type="text"
                        className="form-control"
                        value={header.description}
                        disabled={viewPosted}
                        onChange={(e) => handleHeaderChange('description', e.target.value)}
                      />
                    </div>
                  </div>

                  <div className="d-flex align-items-center justify-content-between mb-2">
                    <h6 className="mb-0">ردیف‌های فروش</h6>
                    {!viewPosted && (
                      <button
                        type="button"
                        className="btn btn-sm btn-outline-secondary d-inline-flex align-items-center gap-1"
                        onClick={addLine}
                      >
                        <Icon name="plus" />
                        <span>ردیف جدید</span>
                      </button>
                    )}
                  </div>

                  <div className="table-responsive">
                    <table className="table align-middle purchase-lines-table">
                      <colgroup>
                        <col className="col-product" />
                        <col className="col-unit" />
                        <col className="col-qty" />
                        <col className="col-price" />
                        <col className="col-total" />
                        <col className="col-total-base" />
                        {viewPosted && <col className="col-total-base" />}
                        {viewPosted && <col className="col-total-base" />}
                        {!viewPosted && <col className="col-actions" />}
                      </colgroup>
                      <thead>
                        <tr>
                          <th className="col-product">محصول</th>
                          <th className="col-unit">واحد</th>
                          <th className="col-qty">مقدار</th>
                          <th className="col-price">قیمت فروش ({invoiceCurrencySymbol || '—'})</th>
                          <th className="col-total">جمع ({invoiceCurrencySymbol || '—'})</th>
                          <th className="col-total-base">جمع ({baseCurrencySymbol || '—'})</th>
                          {viewPosted && <th className="col-total-base">بهای FIFO</th>}
                          {viewPosted && <th className="col-total-base">سود FIFO</th>}
                          {!viewPosted && <th className="col-actions" />}
                        </tr>
                      </thead>
                      <tbody>
                        {computedLines.flatMap((line, index) => {
                          const unitLabel = meaurmentSymbol(line.meaurmentId)
                          const returnedQty = Number(line.returnedQuantity) || 0
                          const qty = Number(line.quantity) || 0
                          const returnRatio = qty > 0 ? returnedQty / qty : 0
                          const returnLineTotal = (Number(line.lineTotal) || 0) * returnRatio
                          const returnLineTotalBase = (Number(line.lineTotalBase) || 0) * returnRatio
                          const returnLineCost =
                            (Number(line.lineCostInBaseCurrency) || 0) * returnRatio
                          const returnLineProfit =
                            (Number(line.lineProfitInBaseCurrency) || 0) * returnRatio

                          const mainRow = (
                            <tr key={index}>
                            <td className="col-product">
                              <SearchableSelect
                                options={products}
                                value={line.productId}
                                onChange={(next) => handleProductChange(index, next)}
                                placeholder="انتخاب..."
                                searchPlaceholder="جستجوی محصول..."
                                size="sm"
                                className="invoice-line-control-height"
                                required
                                disabled={viewPosted}
                              />
                            </td>
                            <td className="col-unit">
                              <select
                                className="form-select form-select-sm invoice-line-control-height"
                                value={line.meaurmentId}
                                required
                                disabled={viewPosted}
                                onChange={(e) => handleMeaurmentChange(index, e.target.value)}
                              >
                                <option value="">—</option>
                                {meaurmentsForProduct(line.productId).map((m) => (
                                  <option key={m.value} value={m.value}>
                                    {m.label}
                                  </option>
                                ))}
                              </select>
                            </td>
                            <td className="col-qty">
                              <PrefixNumberField
                                prefix={meaurmentSymbol(line.meaurmentId)}
                                value={line.quantity}
                                onChange={(next) => handleLineChange(index, 'quantity', next)}
                                min="0"
                                step="any"
                                className="amount-field-sm invoice-line-control-height"
                                required
                                disabled={viewPosted}
                              />
                            </td>
                            <td className="col-price">
                              <AmountField
                                value={line.unitPrice}
                                onChange={(next) => handleLineChange(index, 'unitPrice', next)}
                                symbol={invoiceCurrencySymbol}
                                className="amount-field-sm invoice-line-control-height"
                                min="0"
                                step="any"
                                required
                                disabled={viewPosted}
                              />
                            </td>
                            <td className="col-total text-center">
                              <AmountDisplay value={line.lineTotal} symbol={invoiceCurrencySymbol} />
                            </td>
                            <td className="col-total-base text-center">
                              <AmountDisplay value={line.lineTotalBase} symbol={baseCurrencySymbol} />
                            </td>
                            {viewPosted && (
                              <>
                                <td className="col-total-base text-center">
                                  <AmountDisplay
                                    value={line.lineCostInBaseCurrency}
                                    symbol={baseCurrencySymbol}
                                  />
                                </td>
                                <td className="col-total-base text-center">
                                  <AmountDisplay
                                    value={line.lineProfitInBaseCurrency}
                                    symbol={baseCurrencySymbol}
                                  />
                                </td>
                              </>
                            )}
                            {!viewPosted && (
                              <td className="col-actions">
                                <button
                                  type="button"
                                  className="btn btn-sm btn-outline-danger"
                                  onClick={() => removeLine(index)}
                                >
                                  <Icon name="trash" />
                                </button>
                              </td>
                            )}
                          </tr>
                          )

                          if (!showReturnedQty || returnedQty <= 0) {
                            return [mainRow]
                          }

                          return [
                            mainRow,
                            <tr key={`${index}-return`} className="invoice-return-subrow">
                              <td className="col-product">
                                <span className="text-warning small ps-3">↳ برگشت</span>
                              </td>
                              <td className="col-unit">
                                <span className="small text-muted">{unitLabel || '—'}</span>
                              </td>
                              <td className="col-qty text-center text-warning">
                                {formatAmount(returnedQty)}
                                {unitLabel ? ` ${unitLabel}` : ''}
                              </td>
                              <td className="col-price text-center">
                                <AmountDisplay value={line.unitPrice} symbol={invoiceCurrencySymbol} />
                              </td>
                              <td className="col-total text-center text-warning">
                                <AmountDisplay value={returnLineTotal} symbol={invoiceCurrencySymbol} />
                              </td>
                              <td className="col-total-base text-center text-warning">
                                <AmountDisplay value={returnLineTotalBase} symbol={baseCurrencySymbol} />
                              </td>
                              {viewPosted && (
                                <>
                                  <td className="col-total-base text-center text-warning">
                                    <AmountDisplay
                                      value={returnLineCost}
                                      symbol={baseCurrencySymbol}
                                    />
                                  </td>
                                  <td className="col-total-base text-center text-warning">
                                    <AmountDisplay
                                      value={returnLineProfit}
                                      symbol={baseCurrencySymbol}
                                    />
                                  </td>
                                </>
                              )}
                              {!viewPosted && <td className="col-actions" />}
                            </tr>,
                          ]
                        })}
                      </tbody>
                      <tfoot>
                        <tr>
                          <th colSpan={4} className="text-end">
                            جمع کل
                          </th>
                          <th className="text-center">
                            <AmountDisplay value={totals.total} symbol={invoiceCurrencySymbol} />
                          </th>
                          <th className="text-center">
                            <AmountDisplay value={totals.totalBase} symbol={baseCurrencySymbol} />
                          </th>
                          {viewPosted && (
                            <>
                              <th className="text-center">
                                <AmountDisplay
                                  value={postedTotals?.totalCostInBaseCurrency}
                                  symbol={baseCurrencySymbol}
                                />
                              </th>
                              <th className="text-center">
                                <AmountDisplay
                                  value={postedTotals?.totalProfitInBaseCurrency}
                                  symbol={baseCurrencySymbol}
                                />
                              </th>
                            </>
                          )}
                          {!viewPosted && <th />}
                        </tr>
                      </tfoot>
                    </table>
                  </div>

                  {viewPosted &&
                    lines.some((l) => l.lotAllocations?.length) && (
                      <div className="mt-3">
                        <h6>تخصیص FIFO (از کدام خرید فروخته شده)</h6>
                        {lines.map((line, index) =>
                          line.lotAllocations?.length ? (
                            <div key={index} className="small mb-2">
                              <strong>ردیف {index + 1}</strong>
                              <ul className="mb-0">
                                {line.lotAllocations.map((a, i) => (
                                  <li key={i}>
                                    Lot {a.lotCode} — مقدار پایه {formatAmount(a.quantityInBase)} —
                                    بهای {formatAmount(a.lineCostInBase)}
                                    {a.purchaseInvoiceId
                                      ? ` (خرید #${a.purchaseInvoiceId})`
                                      : ''}
                                  </li>
                                ))}
                              </ul>
                            </div>
                          ) : null,
                        )}
                      </div>
                    )}
                </div>
                <div className="modal-footer">
                  <button type="button" className="btn btn-secondary" onClick={closeModals}>
                    بستن
                  </button>
                  {editId && (
                    <button
                      type="button"
                      className="btn btn-outline-primary d-inline-flex align-items-center gap-2"
                      onClick={() => openPrint(editId)}
                    >
                      <Icon name="print" />
                      <span>چاپ</span>
                    </button>
                  )}
                  {!viewPosted && (
                    <button type="submit" className="btn btn-primary" disabled={submitting}>
                      {submitting
                        ? 'در حال ذخیره...'
                        : isInvoiceStatus
                          ? editId
                            ? 'ذخیره و ثبت فاکتور'
                            : 'ثبت فاکتور فروش'
                          : editId
                            ? 'ذخیره تغییرات'
                            : 'ایجاد فاکتور'}
                    </button>
                  )}
                  {!viewPosted && editId && canEdit && !isInvoiceStatus && !isQuotationStatus && (
                    <button
                      type="button"
                      className="btn btn-success"
                      disabled={submitting}
                      onClick={handlePost}
                    >
                      {String(header.status) === '3'
                        ? 'ثبت نهایی (موجودی + درآمد)'
                        : String(header.status) === '2'
                          ? 'ثبت نهایی (درآمد)'
                          : 'ثبت نهایی'}
                    </button>
                  )}
                  {viewPosted &&
                    canCreate &&
                    documentType === INVOICE_DOCUMENT_TYPE.Invoice &&
                    editId && (
                      <button
                        type="button"
                        className="btn btn-warning d-inline-flex align-items-center gap-2"
                        onClick={() =>
                          setReturnSource({
                            saleInvoiceId: editId,
                            invoiceNumber: invoiceCodePreview,
                          })
                        }
                      >
                        <Icon name="rotate-left" />
                        <span>برگشت از فروش</span>
                      </button>
                    )}
                </div>
              </form>
            </div>
          </div>
        </>
      )}

      {deleteRow && (
        <>
          <div className="modal-backdrop show users-modal-backdrop" onClick={closeModals} />
          <div className="modal show d-block users-modal" tabIndex="-1">
            <div className="modal-dialog modal-dialog-centered">
              <div className="modal-content">
                <div className="modal-header">
                  <h5 className="modal-title">حذف فاکتور</h5>
                  <button type="button" className="btn-close" onClick={closeModals} />
                </div>
                <div className="modal-body">
                  {formError && <div className="alert alert-danger py-2">{formError}</div>}
                  <p>فاکتور «{deleteRow.invoiceNumber}» حذف شود؟</p>
                </div>
                <div className="modal-footer">
                  <button type="button" className="btn btn-secondary" onClick={closeModals}>
                    انصراف
                  </button>
                  <button
                    type="button"
                    className="btn btn-danger"
                    disabled={submitting}
                    onClick={handleDeleteConfirm}
                  >
                    حذف
                  </button>
                </div>
              </div>
            </div>
          </div>
        </>
      )}

      <InvoiceReturnModal
        open={Boolean(returnSource)}
        onClose={() => setReturnSource(null)}
        mode="sale"
        sourceInvoiceId={returnSource?.saleInvoiceId}
        sourceInvoiceNumber={returnSource?.invoiceNumber}
        onSuccess={handleReturnSuccess}
        api={saleInvoicesApi}
      />
    </div>
  )
}

export default SalePage
